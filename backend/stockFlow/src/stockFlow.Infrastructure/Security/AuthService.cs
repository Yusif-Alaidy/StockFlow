using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using stockFlow.Application.Features;
using stockFlow.Application.Common.Errors;
using stockFlow.Infrastructure.Features;
using stockFlow.Infrastructure.Persistence;
namespace stockFlow.Infrastructure.Security;

internal sealed class AuthService(StockFlowDbContext db, Access access, PasswordHasher<AppUser> hasher) : IAuthService
{
    internal static string Hash(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    static string NewToken() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
    static string Email(string email) => email.Trim().ToUpperInvariant();
    static AppException InvalidLogin() => new(ErrorKind.Unauthorized, "invalid_credentials", "Email or password is invalid, or sign-in is temporarily unavailable.");
    public async Task<TokenResult> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        await using var tx = await WriteTransaction.Begin(db, ct);
        var user = await db.Users.SingleOrDefaultAsync(x => x.Email == Email(request.Email), ct);
        if (user is null) { hasher.HashPassword(new AppUser(), request.Password); throw InvalidLogin(); }
        if (!user.IsActive || user.LockedUntil > DateTime.UtcNow) throw InvalidLogin();
        var check = hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (check == PasswordVerificationResult.Failed)
        {
            user.FailedLogins++;
            if (user.FailedLogins >= 5) { user.LockedUntil = DateTime.UtcNow.AddMinutes(15); user.FailedLogins = 0; }
            db.Audit(user.Id, "login_failed", "users", user.Id);
            await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); throw InvalidLogin();
        }
        if (check == PasswordVerificationResult.SuccessRehashNeeded) user.PasswordHash = hasher.HashPassword(user, request.Password);
        user.FailedLogins = 0; user.LockedUntil = null;
        var result = await Issue(user, Guid.NewGuid(), DateTime.UtcNow.AddDays(7), ct);
        db.Audit(user.Id, "login", "users", user.Id);
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
        return result;
    }
    async Task<TokenResult> Issue(AppUser user, Guid familyId, DateTime familyExpiry, CancellationToken ct)
    {
        var bearer = NewToken(); var refresh = NewToken(); var now = DateTime.UtcNow;
        var session = new AuthSession { UserId = user.Id, FamilyId = familyId,
            AccessHash = Hash(bearer), RefreshHash = Hash(refresh), AccessExpiresAt = now.AddMinutes(15), RefreshExpiresAt = familyExpiry };
        db.AuthSessions.Add(session);
        return new(bearer, session.AccessExpiresAt, refresh, familyExpiry, await View(user, ct));
    }
    public async Task<TokenResult> RefreshAsync(string token, CancellationToken ct)
    {
        await using var tx = await WriteTransaction.Begin(db, ct);
        var hash = Hash(token);
        var old = await db.AuthSessions.SingleOrDefaultAsync(x => x.RefreshHash == hash, ct);
        if (old is null) throw InvalidLogin();
        if (old.Revoked)
        {
            await db.AuthSessions.Where(x => x.FamilyId == old.FamilyId).ExecuteUpdateAsync(s => s.SetProperty(x => x.Revoked, true), ct);
            db.Audit(old.UserId, "refresh_reuse", "sessions", old.FamilyId);
            await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); throw InvalidLogin();
        }
        var user = await db.Users.SingleAsync(x => x.Id == old.UserId, ct);
        if (!user.IsActive || old.RefreshExpiresAt <= DateTime.UtcNow) throw InvalidLogin();
        old.Revoked = true;
        var result = await Issue(user, old.FamilyId, old.RefreshExpiresAt, ct);
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return result;
    }
    public async Task<PrincipalInfo?> ValidateAsync(string token, CancellationToken ct)
    {
        if (token.Length != 64 || !Convert.TryFromBase64String(token, new byte[48], out var bytes) || bytes != 48) return null;
        var hash = Hash(token); var now = DateTime.UtcNow;
        return await (from session in db.AuthSessions.AsNoTracking() join user in db.Users.AsNoTracking() on session.UserId equals user.Id
            where session.AccessHash == hash && !session.Revoked && session.AccessExpiresAt > now && user.IsActive
            select new PrincipalInfo(user.Id, user.Role, session.Id)).SingleOrDefaultAsync(ct);
    }
    public async Task LogoutAsync(Guid sessionId, CancellationToken ct)
    {
        await using var tx = await WriteTransaction.Begin(db, ct);
        var session = await db.AuthSessions.SingleOrDefaultAsync(x => x.Id == sessionId && x.UserId == access.Actor, ct) ?? throw Access.Missing();
        await db.AuthSessions.Where(x => x.FamilyId == session.FamilyId).ExecuteUpdateAsync(s => s.SetProperty(x => x.Revoked, true), ct);
        db.Audit(access.Actor, "logout", "sessions", session.FamilyId);
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
    }
    public async Task<UserView> MeAsync(CancellationToken ct) => await View(await access.User(ct), ct);
    internal async Task<UserView> View(AppUser user, CancellationToken ct) => new(user.Id, user.Email, user.Role, user.IsActive,
        await db.UserWarehouses.Where(x => x.UserId == user.Id).OrderBy(x => x.WarehouseId).Select(x => x.WarehouseId).ToArrayAsync(ct), Convert.ToBase64String(user.RowVersion));
    public async Task ChangePasswordAsync(ChangePasswordRequest request, CancellationToken ct)
    {
        await using var tx = await WriteTransaction.Begin(db, ct);
        var user = await access.User(ct);
        if (hasher.VerifyHashedPassword(user, user.PasswordHash, request.CurrentPassword) == PasswordVerificationResult.Failed) throw InvalidLogin();
        ValidatePassword(request.NewPassword);
        user.PasswordHash = hasher.HashPassword(user, request.NewPassword);
        await db.AuthSessions.Where(x => x.UserId == user.Id).ExecuteUpdateAsync(s => s.SetProperty(x => x.Revoked, true), ct);
        db.Audit(user.Id, "password_changed", "users", user.Id);
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
    }
    internal static void ValidatePassword(string password)
    {
        if (password.Length < 12 || password.Length > 256 || !password.Any(char.IsLetter) || !password.Any(char.IsDigit))
            throw Access.Invalid("Password must contain 12–256 characters, a letter and a digit.");
    }
    public async Task BootstrapAsync(string email, string password, CancellationToken ct)
    {
        if (!new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(email) || email.Length > 254) throw Access.Invalid("Invalid email.");
        ValidatePassword(password);
        await using var tx = await WriteTransaction.Begin(db, ct);
        if (await db.Users.AnyAsync(ct)) throw Access.Conflict("already_initialized", "Bootstrap is only available before the first user exists.");
        var user = new AppUser { Email = Email(email), Role = Roles.Admin };
        user.PasswordHash = hasher.HashPassword(user, password); db.Users.Add(user);
        db.Audit(user.Id, "bootstrap", "users", user.Id);
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
    }
}

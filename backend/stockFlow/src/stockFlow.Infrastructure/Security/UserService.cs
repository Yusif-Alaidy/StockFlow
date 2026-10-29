using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using stockFlow.Application.Features;
using stockFlow.Application.Common.Pagination;
using stockFlow.Infrastructure.Features;
using stockFlow.Infrastructure.Persistence;
namespace stockFlow.Infrastructure.Security;
internal sealed class UserService(StockFlowDbContext db, Access access, AuthService auth, PasswordHasher<AppUser> hasher) : IUserService
{
    public async Task<PagedResult<UserView>> ListAsync(PageRequest page, CancellationToken ct)
    {
        await access.RequireRole(ct); var skip = page.GetSkip();
        var users = await db.Users.OrderBy(x => x.Email).Skip(skip).Take(page.PageSize).ToListAsync(ct);
        var views = new List<UserView>(); foreach (var user in users) views.Add(await auth.View(user, ct));
        return new(views, page.PageNumber, page.PageSize, await db.Users.LongCountAsync(ct));
    }
    async Task Validate(string role, Guid[] warehouses, CancellationToken ct)
    {
        if (!Roles.All.Contains(role)) throw Access.Invalid("Unknown role.");
        if (warehouses.Length > 500 || warehouses.Distinct().Count() != warehouses.Length) throw Access.Invalid("Warehouse IDs must be unique; maximum 500.");
        if (await db.Warehouses.CountAsync(x => warehouses.Contains(x.Id) && x.IsActive, ct) != warehouses.Length) throw Access.Invalid("Each warehouse must exist and be active.");
    }
    public async Task<UserView> CreateAsync(CreateUserRequest request, CancellationToken ct)
    {
        await using var tx = await WriteTransaction.Begin(db, ct); await access.RequireRole(ct);
        await Validate(request.Role, request.WarehouseIds, ct); AuthService.ValidatePassword(request.Password);
        var user = new AppUser { Email = request.Email.Trim().ToUpperInvariant(), Role = request.Role };
        user.PasswordHash = hasher.HashPassword(user, request.Password); db.Users.Add(user);
        db.UserWarehouses.AddRange(request.WarehouseIds.Select(id => new UserWarehouse { UserId = user.Id, WarehouseId = id }));
        db.Audit(access.Actor, "created", "users", user.Id);
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return await auth.View(user, ct);
    }
    public async Task<UserView> UpdateAsync(Guid id, UpdateUserRequest request, byte[] version, CancellationToken ct)
    {
        await using var tx = await WriteTransaction.Begin(db, ct); await access.RequireRole(ct);
        await Validate(request.Role, request.WarehouseIds, ct);
        var user = await db.Users.SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw Access.Missing();
        Access.Match(version, user.RowVersion);
        if (user.Role == Roles.Admin && user.IsActive && (!request.IsActive || request.Role != Roles.Admin) &&
            !await db.Users.AnyAsync(x => x.Id != id && x.IsActive && x.Role == Roles.Admin, ct))
            throw Access.Conflict("last_admin", "At least one active administrator must remain.");
        user.Role = request.Role; user.IsActive = request.IsActive;
        await db.UserWarehouses.Where(x => x.UserId == id).ExecuteDeleteAsync(ct);
        db.UserWarehouses.AddRange(request.WarehouseIds.Select(w => new UserWarehouse { UserId = id, WarehouseId = w }));
        // Force a new ETag even when only assignments changed.
        db.Entry(user).Property(x => x.Role).IsModified = true;
        await db.AuthSessions.Where(x => x.UserId == id).ExecuteUpdateAsync(s => s.SetProperty(x => x.Revoked, true), ct);
        db.Audit(access.Actor, "permissions_changed", "users", id);
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return await auth.View(user, ct);
    }
}

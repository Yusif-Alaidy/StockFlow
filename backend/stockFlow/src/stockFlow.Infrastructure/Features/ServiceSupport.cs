using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using stockFlow.Application.Abstractions;
using stockFlow.Application.Common.Errors;
using stockFlow.Application.Features;
using stockFlow.Infrastructure.Persistence;
using stockFlow.Infrastructure.Security;
namespace stockFlow.Infrastructure.Features;

internal sealed class Access(StockFlowDbContext db, ICurrentUser current)
{
    public Guid Actor => current.UserId ?? throw new AppException(ErrorKind.Unauthorized, "authentication_required", "Sign in first.");
    public async Task<AppUser> User(CancellationToken ct) => await db.Users.SingleOrDefaultAsync(x => x.Id == Actor && x.IsActive, ct)
        ?? throw new AppException(ErrorKind.Unauthorized, "authentication_required", "Sign in first.");
    public async Task RequireRole(CancellationToken ct, params string[] roles)
    {
        var user = await User(ct);
        if (user.Role != Roles.Admin && !roles.Contains(user.Role)) throw Forbidden();
    }
    public async Task<Guid[]?> Warehouses(CancellationToken ct)
    {
        var user = await User(ct);
        return user.Role == Roles.Admin ? null : await db.UserWarehouses.Where(x => x.UserId == user.Id).Select(x => x.WarehouseId).ToArrayAsync(ct);
    }
    public async Task Warehouse(Guid id, CancellationToken ct, bool hide = false)
    {
        var allowed = await Warehouses(ct);
        if (allowed is not null && !allowed.Contains(id)) throw hide ? Missing() : Forbidden();
    }
    public static AppException Missing() => new(ErrorKind.NotFound, "not_found", "Resource was not found.");
    public static AppException Forbidden() => new(ErrorKind.Forbidden, "forbidden", "This action is not allowed.");
    public static AppException Conflict(string code, string message) => new(ErrorKind.Conflict, code, message);
    public static AppException Invalid(string message) => new(ErrorKind.Validation, "invalid_request", message);
    public static void Match(byte[] expected, byte[] actual)
    {
        if (!expected.SequenceEqual(actual)) throw new AppException(ErrorKind.PreconditionFailed, "stale_version", "Reload the resource and retry with its current ETag.");
    }
}
internal static class WriteTransaction
{
    // A deliberately simple MVP policy: all application writes in one database are serialized.
    // Lock ownership is Transaction, released by commit/rollback/disposal, including exceptions.
    public static async Task<IDbContextTransaction> Begin(StockFlowDbContext db, CancellationToken ct)
    {
        var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        try
        {
            await db.Database.ExecuteSqlRawAsync("DECLARE @result int; EXEC @result = sys.sp_getapplock @Resource=N'StockFlow:Write', @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=10000; IF @result < 0 THROW 51001, 'StockFlow write lock unavailable', 1;", ct);
            return tx;
        }
        catch { await tx.DisposeAsync(); throw; }
    }
    public static void Audit(this StockFlowDbContext db, Guid? actor, string action, string resource, Guid? id) =>
        db.AuditEntries.Add(new AuditEntry { ActorId = actor, Action = action, Resource = resource, ResourceId = id });
}

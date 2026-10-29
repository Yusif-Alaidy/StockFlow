using System.ComponentModel.DataAnnotations;
using stockFlow.Application.Common.Pagination;
namespace stockFlow.Application.Features;

public static class Roles
{
    public const string Admin = "Admin", PurchasingOfficer = "PurchasingOfficer", WarehouseKeeper = "WarehouseKeeper", Viewer = "Viewer";
    public static readonly string[] All = [Admin, PurchasingOfficer, WarehouseKeeper, Viewer];
}
public record LoginRequest([Required, EmailAddress, MaxLength(254)] string Email, [Required, MaxLength(256)] string Password);
public record RefreshRequest([Required, MaxLength(200)] string RefreshToken);
public record TokenResult(string AccessToken, DateTime AccessExpiresAt, string RefreshToken, DateTime RefreshExpiresAt, UserView User);
public record UserView(Guid Id, string Email, string Role, bool IsActive, IReadOnlyList<Guid> WarehouseIds, string Version);
public record CreateUserRequest([Required, EmailAddress, MaxLength(254)] string Email, [Required, MinLength(12), MaxLength(256)] string Password, [Required] string Role, [Required] Guid[] WarehouseIds);
public record UpdateUserRequest([Required] string Role, bool IsActive, [Required] Guid[] WarehouseIds);
public record ChangePasswordRequest([Required, MaxLength(256)] string CurrentPassword, [Required, MinLength(12), MaxLength(256)] string NewPassword);
public record PrincipalInfo(Guid UserId, string Role, Guid SessionId);
public interface IAuthService
{
    Task<TokenResult> LoginAsync(LoginRequest request, CancellationToken ct);
    Task<TokenResult> RefreshAsync(string token, CancellationToken ct);
    Task<PrincipalInfo?> ValidateAsync(string accessToken, CancellationToken ct);
    Task LogoutAsync(Guid sessionId, CancellationToken ct);
    Task<UserView> MeAsync(CancellationToken ct);
    Task ChangePasswordAsync(ChangePasswordRequest request, CancellationToken ct);
    Task BootstrapAsync(string email, string password, CancellationToken ct);
}
public interface IUserService
{
    Task<PagedResult<UserView>> ListAsync(PageRequest page, CancellationToken ct);
    Task<UserView> CreateAsync(CreateUserRequest request, CancellationToken ct);
    Task<UserView> UpdateAsync(Guid id, UpdateUserRequest request, byte[] version, CancellationToken ct);
}
public record MasterInput([Required, MaxLength(200)] string Name, [MaxLength(50)] string? Code = null,
    Guid? BranchId = null, Guid? CategoryId = null, Guid? UnitId = null,
    [EmailAddress, MaxLength(254)] string? Email = null, [MaxLength(30)] string? Phone = null, [MaxLength(500)] string? Address = null);
public record MasterView(Guid Id, string Name, string? Code, bool IsActive, Guid? BranchId, Guid? CategoryId, Guid? UnitId, string? Email, string? Phone, string? Address, string Version);
public interface IMasterService
{
    Task<PagedResult<MasterView>> ListAsync(string resource, PageRequest page, string? search, bool? active, CancellationToken ct);
    Task<MasterView> GetAsync(string resource, Guid id, CancellationToken ct);
    Task<MasterView> CreateAsync(string resource, MasterInput request, CancellationToken ct);
    Task<MasterView> UpdateAsync(string resource, Guid id, MasterInput request, byte[] version, CancellationToken ct);
    Task<MasterView> SetActiveAsync(string resource, Guid id, bool active, byte[] version, CancellationToken ct);
}
public record DocumentLineInput(Guid ProductId, decimal Quantity, decimal UnitPrice = 0);
public record DocumentInput(Guid WarehouseId, Guid? SupplierId, Guid? DestinationWarehouseId,
    [MaxLength(300)] string? Reason, [MaxLength(1000)] string? Notes, [Required, MinLength(1), MaxLength(200)] DocumentLineInput[] Lines);
public record ReceiptInput(Guid PurchaseOrderId, [MaxLength(1000)] string? Notes);
public record DocumentEdit([MaxLength(1000)] string? Notes, [Required, MinLength(1), MaxLength(200)] DocumentLineInput[] Lines);
public record DocumentLineView(Guid Id, Guid ProductId, decimal Quantity, decimal UnitPrice);
public record DocumentView(Guid Id, string Number, string Kind, string Status, Guid WarehouseId, Guid? DestinationWarehouseId,
    Guid? SupplierId, Guid? PurchaseOrderId, string? Reason, string? Notes, DateTime CreatedAt, IReadOnlyList<DocumentLineView> Lines, string Version);
public interface IDocumentService
{
    Task<PagedResult<DocumentView>> ListAsync(string kind, PageRequest page, Guid? warehouseId, string? status, CancellationToken ct);
    Task<DocumentView> GetAsync(string kind, Guid id, CancellationToken ct);
    Task<DocumentView> CreateAsync(string kind, DocumentInput request, CancellationToken ct);
    Task<DocumentView> ReceiveAsync(ReceiptInput request, CancellationToken ct);
    Task<DocumentView> EditAsync(string kind, Guid id, DocumentEdit request, byte[] version, CancellationToken ct);
    Task<DocumentView> ActAsync(string kind, Guid id, string action, byte[] version, CancellationToken ct);
}
public record BalanceView(Guid WarehouseId, Guid ProductId, decimal Quantity);
public record MovementView(Guid Id, Guid WarehouseId, Guid ProductId, string DocumentType, Guid DocumentId, decimal Quantity, DateTime OccurredAt);
public record ReconciliationView(Guid WarehouseId, Guid ProductId, decimal Balance, decimal Ledger, decimal Difference);
public record DashboardView(long Products, long Warehouses, long DraftDocuments, long StockedPairs);
public record AuditView(Guid Id, Guid? ActorId, string Action, string Resource, Guid? ResourceId, DateTime At);
public interface IReportService
{
    Task<PagedResult<BalanceView>> BalancesAsync(PageRequest page, Guid? warehouseId, Guid? productId, CancellationToken ct);
    Task<PagedResult<MovementView>> MovementsAsync(PageRequest page, Guid? warehouseId, Guid? productId, DateTime? from, DateTime? to, CancellationToken ct);
    Task<PagedResult<ReconciliationView>> ReconcileAsync(PageRequest page, Guid? warehouseId, CancellationToken ct);
    Task<DashboardView> DashboardAsync(CancellationToken ct);
    Task<PagedResult<AuditView>> AuditAsync(PageRequest page, CancellationToken ct);
}

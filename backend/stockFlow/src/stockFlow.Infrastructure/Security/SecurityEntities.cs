namespace stockFlow.Infrastructure.Security;
public sealed class AppUser
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string Role { get; set; } = "Viewer";
    public bool IsActive { get; set; } = true;
    public int FailedLogins { get; set; }
    public DateTime? LockedUntil { get; set; }
    public byte[] RowVersion { get; set; } = [];
}
public sealed class UserWarehouse
{
    public Guid UserId { get; set; }
    public Guid WarehouseId { get; set; }
}
public sealed class AuthSession
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid FamilyId { get; set; }
    public Guid UserId { get; set; }
    public string AccessHash { get; set; } = "";
    public string RefreshHash { get; set; } = "";
    public DateTime AccessExpiresAt { get; set; }
    public DateTime RefreshExpiresAt { get; set; }
    public bool Revoked { get; set; }
}
public sealed class AuditEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? ActorId { get; set; }
    public string Action { get; set; } = "";
    public string Resource { get; set; } = "";
    public Guid? ResourceId { get; set; }
    public DateTime At { get; set; } = DateTime.UtcNow;
}

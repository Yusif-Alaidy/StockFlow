using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using stockFlow.Domain.Entities;

namespace stockFlow.Infrastructure.Persistence;

public class StockFlowDbContext : DbContext
{
    public StockFlowDbContext(DbContextOptions<StockFlowDbContext> options) : base(options) { }

    public DbSet<stockFlow.Infrastructure.Security.AppUser> Users => Set<stockFlow.Infrastructure.Security.AppUser>();
    public DbSet<stockFlow.Infrastructure.Security.UserWarehouse> UserWarehouses => Set<stockFlow.Infrastructure.Security.UserWarehouse>();
    public DbSet<stockFlow.Infrastructure.Security.AuthSession> AuthSessions => Set<stockFlow.Infrastructure.Security.AuthSession>();
    public DbSet<stockFlow.Infrastructure.Security.AuditEntry> AuditEntries => Set<stockFlow.Infrastructure.Security.AuditEntry>();

    public DbSet<Branch> Branches => Set<Branch>();
    public DbSet<Warehouse> Warehouses => Set<Warehouse>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Unit> Units => Set<Unit>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();
    public DbSet<StockOpening> StockOpenings => Set<StockOpening>();
    public DbSet<StockReceipt> StockReceipts => Set<StockReceipt>();
    public DbSet<StockIssue> StockIssues => Set<StockIssue>();
    public DbSet<StockTransfer> StockTransfers => Set<StockTransfer>();
    public DbSet<PurchaseOrderLine> PurchaseOrderLines => Set<PurchaseOrderLine>();
    public DbSet<StockOpeningLine> StockOpeningLines => Set<StockOpeningLine>();
    public DbSet<StockReceiptLine> StockReceiptLines => Set<StockReceiptLine>();
    public DbSet<StockIssueLine> StockIssueLines => Set<StockIssueLine>();
    public DbSet<StockTransferLine> StockTransferLines => Set<StockTransferLine>();
    public DbSet<StockBalance> StockBalances => Set<StockBalance>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(StockFlowDbContext).Assembly);
        var users = modelBuilder.Entity<stockFlow.Infrastructure.Security.AppUser>();
        users.ToTable("Users"); users.HasKey(x => x.Id);
        users.Property(x => x.Email).HasMaxLength(254).IsRequired(); users.HasIndex(x => x.Email).IsUnique();
        users.Property(x => x.PasswordHash).HasMaxLength(512).IsRequired();
        users.Property(x => x.Role).HasMaxLength(30).IsRequired(); users.Property(x => x.RowVersion).IsRowVersion();
        users.Property(x => x.LockedUntil).HasConversion(value => value,
            value => value.HasValue ? (DateTime?)DateTime.SpecifyKind(value.Value, DateTimeKind.Utc) : null);
        var access = modelBuilder.Entity<stockFlow.Infrastructure.Security.UserWarehouse>();
        access.ToTable("UserWarehouses"); access.HasKey(x => new { x.UserId, x.WarehouseId });
        access.HasOne<stockFlow.Infrastructure.Security.AppUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        access.HasOne<Warehouse>().WithMany().HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict);
        var sessions = modelBuilder.Entity<stockFlow.Infrastructure.Security.AuthSession>();
        sessions.ToTable("AuthSessions"); sessions.HasKey(x => x.Id);
        sessions.Property(x => x.AccessHash).HasMaxLength(64).IsRequired(); sessions.HasIndex(x => x.AccessHash).IsUnique();
        sessions.Property(x => x.RefreshHash).HasMaxLength(64).IsRequired(); sessions.HasIndex(x => x.RefreshHash).IsUnique();
        sessions.HasIndex(x => x.FamilyId);
        sessions.Property(x => x.AccessExpiresAt).HasConversion(value => value, value => DateTime.SpecifyKind(value, DateTimeKind.Utc));
        sessions.Property(x => x.RefreshExpiresAt).HasConversion(value => value, value => DateTime.SpecifyKind(value, DateTimeKind.Utc));
        sessions.HasOne<stockFlow.Infrastructure.Security.AppUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        var audit = modelBuilder.Entity<stockFlow.Infrastructure.Security.AuditEntry>();
        audit.ToTable("AuditEntries"); audit.HasKey(x => x.Id);
        audit.Property(x => x.Action).HasMaxLength(60).IsRequired(); audit.Property(x => x.Resource).HasMaxLength(60).IsRequired();
        audit.HasIndex(x => x.At);
        audit.Property(x => x.At).HasConversion(value => value, value => DateTime.SpecifyKind(value, DateTimeKind.Utc));
        modelBuilder.HasSequence<long>("DocumentNumbers", "dbo").StartsAt(1).IncrementsBy(1);
    }

    // Protect the movement ledger when saving through this context.
    // ExecuteSqlRaw, ExecuteUpdate/Delete, and external SQL bypass this guard.
    private void ValidateMovementChanges()
    {
        ChangeTracker.DetectChanges();
        if (ChangeTracker.Entries<stockFlow.Infrastructure.Security.AuditEntry>().Any(entry => entry.State is EntityState.Modified or EntityState.Deleted))
            throw new InvalidOperationException("Audit entries are append-only.");
        if (ChangeTracker.Entries<StockMovement>().Any(entry =>
            entry.State == EntityState.Modified || entry.State == EntityState.Deleted))
            throw new InvalidOperationException("Stock movements are append-only.");
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ValidateMovementChanges();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        ValidateMovementChanges();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }
}

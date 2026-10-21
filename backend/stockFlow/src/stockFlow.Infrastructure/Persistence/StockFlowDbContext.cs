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
        modelBuilder.HasSequence<long>("DocumentNumbers", "dbo").StartsAt(1).IncrementsBy(1);
    }

    // Protect the movement ledger when saving through this context.
    // ExecuteSqlRaw, ExecuteUpdate/Delete, and external SQL bypass this guard.
    private void ValidateMovementChanges()
    {
        ChangeTracker.DetectChanges();
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

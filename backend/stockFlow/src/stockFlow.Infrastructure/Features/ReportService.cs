using Microsoft.EntityFrameworkCore;
using stockFlow.Application.Common.Pagination;
using stockFlow.Application.Features;
using stockFlow.Infrastructure.Persistence;
namespace stockFlow.Infrastructure.Features;
internal sealed class ReportService(StockFlowDbContext db, Access access) : IReportService
{
    async Task<Guid[]?> Scope(Guid? warehouse, CancellationToken ct)
    {
        if (warehouse is Guid id) await access.Warehouse(id, ct);
        return await access.Warehouses(ct);
    }
    public async Task<PagedResult<BalanceView>> BalancesAsync(PageRequest page, Guid? warehouseId, Guid? productId, CancellationToken ct)
    {
        var skip = page.GetSkip(); var allowed = await Scope(warehouseId, ct); var query = db.StockBalances.AsNoTracking();
        if (allowed is not null) query = query.Where(x => allowed.Contains(x.WarehouseId));
        if (warehouseId.HasValue) query = query.Where(x => x.WarehouseId == warehouseId.Value);
        if (productId.HasValue) query = query.Where(x => x.ProductId == productId.Value);
        return new(await query.OrderBy(x => x.WarehouseId).ThenBy(x => x.ProductId).Skip(skip).Take(page.PageSize)
            .Select(x => new BalanceView(x.WarehouseId, x.ProductId, x.Quantity)).ToListAsync(ct), page.PageNumber, page.PageSize, await query.LongCountAsync(ct));
    }
    public async Task<PagedResult<MovementView>> MovementsAsync(PageRequest page, Guid? warehouseId, Guid? productId, DateTime? from, DateTime? to, CancellationToken ct)
    {
        if ((from.HasValue && from.Value.Kind != DateTimeKind.Utc) || (to.HasValue && to.Value.Kind != DateTimeKind.Utc) || from > to)
            throw Access.Invalid("Use UTC dates ending in Z and from <= to.");
        var skip = page.GetSkip(); var allowed = await Scope(warehouseId, ct); var query = db.StockMovements.AsNoTracking();
        if (allowed is not null) query = query.Where(x => allowed.Contains(x.WarehouseId));
        if (warehouseId.HasValue) query = query.Where(x => x.WarehouseId == warehouseId.Value);
        if (productId.HasValue) query = query.Where(x => x.ProductId == productId.Value);
        if (from.HasValue) query = query.Where(x => x.OccurredAt >= from.Value);
        if (to.HasValue) query = query.Where(x => x.OccurredAt < to.Value);
        var total = await query.LongCountAsync(ct);
        var rows = await query.OrderByDescending(x => x.OccurredAt).ThenBy(x => x.Id).Skip(skip).Take(page.PageSize).ToListAsync(ct);
        return new(rows.Select(x => new MovementView(x.Id, x.WarehouseId, x.ProductId, x.DocumentType.ToString(), x.DocumentId, x.SignedQuantity, x.OccurredAt)).ToList(), page.PageNumber, page.PageSize, total);
    }
    public async Task<PagedResult<ReconciliationView>> ReconcileAsync(PageRequest page, Guid? warehouseId, CancellationToken ct)
    {
        // Take the same application lock as posting so balance/ledger comparisons are consistent.
        await using var tx = await WriteTransaction.Begin(db, ct);
        var skip = page.GetSkip(); var allowed = await Scope(warehouseId, ct);
        var balances = db.StockBalances.AsNoTracking(); var movements = db.StockMovements.AsNoTracking();
        if (allowed is not null) { balances = balances.Where(x => allowed.Contains(x.WarehouseId)); movements = movements.Where(x => allowed.Contains(x.WarehouseId)); }
        if (warehouseId.HasValue) { balances = balances.Where(x => x.WarehouseId == warehouseId.Value); movements = movements.Where(x => x.WarehouseId == warehouseId.Value); }
        var pairs = balances.Select(x => new { x.WarehouseId, x.ProductId }).Union(movements.Select(x => new { x.WarehouseId, x.ProductId }));
        var total = await pairs.LongCountAsync(ct);
        var query = pairs.OrderBy(x => x.WarehouseId).ThenBy(x => x.ProductId).Skip(skip).Take(page.PageSize).Select(pair => new {
            pair.WarehouseId, pair.ProductId,
            Balance = balances.Where(x => x.WarehouseId == pair.WarehouseId && x.ProductId == pair.ProductId).Select(x => (decimal?)x.Quantity).Sum() ?? 0,
            Ledger = movements.Where(x => x.WarehouseId == pair.WarehouseId && x.ProductId == pair.ProductId).Select(x => (decimal?)x.SignedQuantity).Sum() ?? 0 });
        var rows = await query.ToListAsync(ct);
        await tx.CommitAsync(ct);
        return new(rows.Select(x => new ReconciliationView(x.WarehouseId, x.ProductId, x.Balance, x.Ledger, x.Balance - x.Ledger)).ToList(), page.PageNumber, page.PageSize, total);
    }
    public async Task<DashboardView> DashboardAsync(CancellationToken ct)
    {
        var allowed = await access.Warehouses(ct);
        var warehouses = db.Warehouses.Where(x => x.IsActive); var balances = db.StockBalances.AsQueryable();
        var po = db.PurchaseOrders.Where(x => x.Status == stockFlow.Domain.Enums.PurchaseOrderStatus.Draft);
        var op = db.StockOpenings.Where(x => x.Status == stockFlow.Domain.Enums.StockDocumentStatus.Draft);
        var rc = db.StockReceipts.Where(x => x.Status == stockFlow.Domain.Enums.StockDocumentStatus.Draft);
        var issues = db.StockIssues.Where(x => x.Status == stockFlow.Domain.Enums.StockDocumentStatus.Draft);
        var tr = db.StockTransfers.Where(x => x.Status == stockFlow.Domain.Enums.StockDocumentStatus.Draft);
        if (allowed is not null)
        {
            warehouses = warehouses.Where(x => allowed.Contains(x.Id)); balances = balances.Where(x => allowed.Contains(x.WarehouseId));
            po = po.Where(x => allowed.Contains(x.WarehouseId)); op = op.Where(x => allowed.Contains(x.WarehouseId));
            rc = rc.Where(x => allowed.Contains(x.WarehouseId)); issues = issues.Where(x => allowed.Contains(x.WarehouseId));
            tr = tr.Where(x => allowed.Contains(x.FromWarehouseId) && allowed.Contains(x.ToWarehouseId));
        }
        return new(await db.Products.LongCountAsync(x => x.IsActive, ct), await warehouses.LongCountAsync(ct),
            await po.LongCountAsync(ct) + await op.LongCountAsync(ct) + await rc.LongCountAsync(ct) + await issues.LongCountAsync(ct) + await tr.LongCountAsync(ct),
            await balances.LongCountAsync(x => x.Quantity > 0, ct));
    }
    public async Task<PagedResult<AuditView>> AuditAsync(PageRequest page, CancellationToken ct)
    {
        await access.RequireRole(ct); var skip = page.GetSkip();
        return new(await db.AuditEntries.AsNoTracking().OrderByDescending(x => x.At).ThenBy(x => x.Id).Skip(skip).Take(page.PageSize)
            .Select(x => new AuditView(x.Id, x.ActorId, x.Action, x.Resource, x.ResourceId, x.At)).ToListAsync(ct), page.PageNumber, page.PageSize, await db.AuditEntries.LongCountAsync(ct));
    }
}

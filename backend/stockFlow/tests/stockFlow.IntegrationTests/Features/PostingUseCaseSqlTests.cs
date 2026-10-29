using Microsoft.EntityFrameworkCore;
using stockFlow.Application.Common.Errors;
using stockFlow.Application.Common.Pagination;
using stockFlow.Application.Features;
using Xunit;
namespace stockFlow.IntegrationTests.Features;
[Trait("Category", "Sql")]
public sealed class PostingUseCaseSqlTests
{
    [Fact] public async Task Full_workflow_reconciles_and_post_retry_does_not_duplicate()
    {
        await using var f = new BackendFixture(); await f.Initialize(); var (a, b, p, supplier) = await f.Catalog();
        var op = await f.Create("openings", new(a, null, null, null, null, [new(p, 10)])); await f.Act(op, "post");
        var po = await f.Create("purchase-orders", new(a, supplier, null, null, null, [new(p, 5, 2)])); await f.Act(po, "submit");
        var rc = await f.Admin<IDocumentService, DocumentView>(s => s.ReceiveAsync(new(po.Id, null), default));
        var same = await f.Admin<IDocumentService, DocumentView>(s => s.ReceiveAsync(new(po.Id, null), default)); Assert.Equal(rc.Id, same.Id);
        await f.Act(rc, "post"); await f.Act(rc, "post");
        var issue = await f.Create("issues", new(a, null, null, "Usage", null, [new(p, 3)])); await f.Act(issue, "post");
        var transfer = await f.Create("transfers", new(a, null, b, null, null, [new(p, 4)])); await f.Act(transfer, "post");
        await using var db = f.Database.CreateContext();
        Assert.Equal(8m, await db.StockBalances.Where(x => x.WarehouseId == a).Select(x => x.Quantity).SingleAsync());
        Assert.Equal(4m, await db.StockBalances.Where(x => x.WarehouseId == b).Select(x => x.Quantity).SingleAsync());
        Assert.Equal(5, await db.StockMovements.CountAsync());
        var report = await f.Admin<IReportService, PagedResult<ReconciliationView>>(s => s.ReconcileAsync(new(), null, default));
        Assert.All(report.Items, x => Assert.Equal(0, x.Difference));
        Assert.Contains(await db.AuditEntries.Select(x => x.Action).ToListAsync(), x => x == "post");
    }
    [Fact] public async Task Concurrent_issues_cannot_overspend_and_failed_post_has_no_partial_side_effects()
    {
        await using var f = new BackendFixture(); await f.Initialize(); var (a, _, p, _) = await f.Catalog();
        await f.Act(await f.Create("openings", new(a, null, null, null, null, [new(p, 10)])), "post");
        var x = await f.Create("issues", new(a, null, null, "X", null, [new(p, 7)]));
        var y = await f.Create("issues", new(a, null, null, "Y", null, [new(p, 7)]));
        async Task<bool> Try(DocumentView doc) { try { await f.Act(doc, "post"); return true; } catch (AppException ex) when (ex.Code == "insufficient_stock") { return false; } }
        var results = await Task.WhenAll(Try(x), Try(y)); Assert.Single(results, x => x);
        await using var db = f.Database.CreateContext();
        Assert.Equal(3m, await db.StockBalances.Select(x => x.Quantity).SingleAsync()); Assert.Equal(2, await db.StockMovements.CountAsync());
        Assert.Equal(1, await db.StockIssues.CountAsync(x => x.Status == stockFlow.Domain.Enums.StockDocumentStatus.Draft));
        Assert.Equal(2, await db.AuditEntries.CountAsync(x => x.Action == "post"));
    }
    [Fact] public async Task Concurrent_post_of_same_document_is_idempotent()
    {
        await using var f = new BackendFixture(); await f.Initialize(); var (a, _, p, _) = await f.Catalog();
        var op = await f.Create("openings", new(a, null, null, null, null, [new(p, 10)]));
        await Task.WhenAll(f.Act(op, "post"), f.Act(op, "post"));
        await using var db = f.Database.CreateContext(); Assert.Single(await db.StockMovements.ToListAsync());
        Assert.Equal(10m, await db.StockBalances.Select(x => x.Quantity).SingleAsync());
    }
    [Fact] public async Task Stale_edit_and_opening_after_zero_balance_are_rejected()
    {
        await using var f = new BackendFixture(); await f.Initialize(); var (a, _, p, _) = await f.Catalog();
        var op = await f.Create("openings", new(a, null, null, null, null, [new(p, 10)]));
        await f.Admin<IDocumentService, DocumentView>(s => s.EditAsync(op.Kind, op.Id, new("Changed", [new(p, 10)]), Convert.FromBase64String(op.Version), default));
        var stale = await Assert.ThrowsAsync<AppException>(() => f.Act(op, "post")); Assert.Equal(ErrorKind.PreconditionFailed, stale.Kind);
        var current = await f.Admin<IDocumentService, DocumentView>(s => s.GetAsync(op.Kind, op.Id, default)); await f.Act(current, "post");
        await f.Act(await f.Create("issues", new(a, null, null, "All", null, [new(p, 10)])), "post");
        var another = await f.Create("openings", new(a, null, null, null, null, [new(p, 1)]));
        Assert.Equal("opening_exists", (await Assert.ThrowsAsync<AppException>(() => f.Act(another, "post"))).Code);
    }
    [Fact] public async Task Failed_transfer_rolls_back_every_line_balance_ledger_state_and_audit()
    {
        await using var f = new BackendFixture(); await f.Initialize(); var (a, b, p, _) = await f.Catalog();
        await f.Act(await f.Create("openings", new(a, null, null, null, null, [new(p, 2)])), "post");
        var tr = await f.Create("transfers", new(a, null, b, null, null, [new(p, 3)]));
        await Assert.ThrowsAsync<AppException>(() => f.Act(tr, "post"));
        await using var db = f.Database.CreateContext(); Assert.Equal(2m, await db.StockBalances.Select(x => x.Quantity).SingleAsync());
        Assert.Single(await db.StockMovements.ToListAsync()); Assert.Equal(stockFlow.Domain.Enums.StockDocumentStatus.Draft, (await db.StockTransfers.SingleAsync()).Status);
        Assert.False(await db.AuditEntries.AnyAsync(x => x.ResourceId == tr.Id && x.Action == "post"));
    }
    [Fact] public async Task Multi_line_failure_does_not_keep_an_earlier_successful_delta()
    {
        await using var f = new BackendFixture(); await f.Initialize(); var (a, b, p, _) = await f.Catalog();
        var product = await f.Admin<IMasterService, MasterView>(s => s.GetAsync("products", p, default));
        var second = await f.Master("products", new("Second", "SECOND", CategoryId: product.CategoryId, UnitId: product.UnitId));
        var ids = new[] { p, second.Id }.OrderBy(x => x).ToArray();
        await f.Act(await f.Create("openings", new(a, null, null, null, null, [new(ids[0], 10)])), "post");
        var transfer = await f.Create("transfers", new(a, null, b, null, null, [new(ids[0], 4), new(ids[1], 4)]));
        Assert.Equal("insufficient_stock", (await Assert.ThrowsAsync<AppException>(() => f.Act(transfer, "post"))).Code);
        await using var db = f.Database.CreateContext();
        Assert.Single(await db.StockBalances.ToListAsync()); Assert.Equal(10m, await db.StockBalances.Select(x => x.Quantity).SingleAsync());
        Assert.Single(await db.StockMovements.ToListAsync());
        Assert.False(await db.AuditEntries.AnyAsync(x => x.ResourceId == transfer.Id && x.Action == "post"));
    }
    [Fact] public async Task Opposing_transfers_complete_without_losing_stock()
    {
        await using var f = new BackendFixture(); await f.Initialize(); var (a, b, p, _) = await f.Catalog();
        await f.Act(await f.Create("openings", new(a, null, null, null, null, [new(p, 10)])), "post");
        await f.Act(await f.Create("openings", new(b, null, null, null, null, [new(p, 10)])), "post");
        var x = await f.Create("transfers", new(a, null, b, null, null, [new(p, 7)]));
        var y = await f.Create("transfers", new(b, null, a, null, null, [new(p, 7)]));
        await Task.WhenAll(f.Act(x, "post"), f.Act(y, "post"));
        await using var db = f.Database.CreateContext(); Assert.All(await db.StockBalances.ToListAsync(), x => Assert.Equal(10m, x.Quantity));
        Assert.Equal(6, await db.StockMovements.CountAsync());
    }
}

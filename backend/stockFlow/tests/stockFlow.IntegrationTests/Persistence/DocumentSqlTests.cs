using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using stockFlow.Domain.Entities;
using stockFlow.Domain.Enums;
using stockFlow.Infrastructure.Persistence;
using Xunit;

namespace stockFlow.IntegrationTests.Persistence;

[Trait("Category", "SqlServer")]
public class DocumentSqlTests
{
    private static readonly DateTime Now = new(2026, 10, 21, 12, 0, 0, DateTimeKind.Utc);
    private static Guid Actor => Guid.NewGuid();

    private static async Task<(Guid Warehouse, Guid OtherWarehouse, Guid Product, Guid Supplier)> SeedAsync(StockFlowDbContext db)
    {
        var branch = new Branch("B1", "Main", Now);
        var warehouse = new Warehouse(branch.Id, "W1", "Main warehouse", Now);
        var other = new Warehouse(branch.Id, "W2", "Other warehouse", Now);
        var category = new Category("Furniture", Now);
        var unit = new Unit("PCS", "Piece", Now);
        var product = new Product("SKU1", "Chair", category.Id, unit.Id, Now);
        var supplier = new Supplier("S1", "Supplier", Now);
        db.AddRange(branch, warehouse, other, category, unit, product, supplier);
        await db.SaveChangesAsync();
        return (warehouse.Id, other.Id, product.Id, supplier.Id);
    }

    private static void AssertUnique(DbUpdateException error)
    {
        var sql = Assert.IsType<SqlException>(error.InnerException);
        Assert.True(sql.Number == 2601 || sql.Number == 2627, error.ToString());
    }
    private static void AssertConstraint(DbUpdateException error) =>
        Assert.Equal(547, Assert.IsType<SqlException>(error.InnerException).Number);

    [Fact]
    public async Task Migrations_roundtrip_all_document_types_and_private_line_collections()
    {
        await using var database = new SqlTestDatabase(); await database.InitializeAsync();
        Guid poId, receiptId, openingId, issueId, transferId;
        await using (var db = database.CreateContext())
        {
            var ids = await SeedAsync(db);
            var po = new PurchaseOrder("PO-1", ids.Supplier, ids.Warehouse, Actor, Now);
            po.AddLine(ids.Product, 1.125m, 12.34m, Now); po.Submit(Actor, Now);
            var receipt = StockReceipt.FromPurchaseOrder("RC-1", po, Actor, Now);
            var opening = new StockOpening("OP-1", ids.Warehouse, Actor, Now);
            opening.AddLine(ids.Product, 2m, Now);
            var issue = new StockIssue("IS-1", ids.Warehouse, "Damaged", Actor, Now);
            issue.AddLine(ids.Product, 1m, Now);
            var transfer = new StockTransfer("TR-1", ids.Warehouse, ids.OtherWarehouse, Actor, Now);
            transfer.AddLine(ids.Product, 1m, Now);
            db.AddRange(po, receipt, opening, issue, transfer);
            await db.SaveChangesAsync();
            poId = po.Id; receiptId = receipt.Id; openingId = opening.Id; issueId = issue.Id; transferId = transfer.Id;
        }
        await using var read = database.CreateContext();
        var loaded = await read.PurchaseOrders.Include(x => x.Lines).SingleAsync(x => x.Id == poId);
        Assert.Equal(1.125m, loaded.Lines.Single().Quantity);
        Assert.Equal(12.34m, loaded.Lines.Single().UnitPrice);
        Assert.Equal(13.88m, loaded.Total);
        Assert.Equal(DateTimeKind.Utc, loaded.CreatedAt.Kind);
        Assert.Equal(DateTimeKind.Utc, loaded.SubmittedAt!.Value.Kind);
        Assert.Equal(8, loaded.RowVersion.Length);
        Assert.Single((await read.StockReceipts.Include(x => x.Lines).SingleAsync(x => x.Id == receiptId)).Lines);
        Assert.Single((await read.StockOpenings.Include(x => x.Lines).SingleAsync(x => x.Id == openingId)).Lines);
        Assert.Single((await read.StockIssues.Include(x => x.Lines).SingleAsync(x => x.Id == issueId)).Lines);
        Assert.Single((await read.StockTransfers.Include(x => x.Lines).SingleAsync(x => x.Id == transferId)).Lines);
    }

    [Fact]
    public async Task Duplicate_document_number_is_rejected_by_SQL()
    {
        await using var database = new SqlTestDatabase(); await database.InitializeAsync();
        await using var db = database.CreateContext(); var ids = await SeedAsync(db);
        db.PurchaseOrders.Add(new PurchaseOrder("SAME", ids.Supplier, ids.Warehouse, Actor, Now)); await db.SaveChangesAsync();
        db.PurchaseOrders.Add(new PurchaseOrder("SAME", ids.Supplier, ids.Warehouse, Actor, Now));
        AssertUnique(await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync()));
    }

    [Fact]
    public async Task Two_receipts_for_same_PO_are_rejected_by_SQL()
    {
        await using var database = new SqlTestDatabase(); await database.InitializeAsync();
        await using var db = database.CreateContext(); var ids = await SeedAsync(db);
        var po = new PurchaseOrder("PO", ids.Supplier, ids.Warehouse, Actor, Now);
        po.AddLine(ids.Product, 1m, 1m, Now); po.Submit(Actor, Now);
        db.Add(po); await db.SaveChangesAsync();
        db.Add(StockReceipt.FromPurchaseOrder("R1", po, Actor, Now)); await db.SaveChangesAsync();
        db.Add(StockReceipt.FromPurchaseOrder("R2", po, Actor, Now));
        AssertUnique(await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync()));
    }

    [Fact]
    public async Task SQL_rejects_duplicate_product_even_if_domain_is_bypassed()
    {
        await using var database = new SqlTestDatabase(); await database.InitializeAsync();
        await using var db = database.CreateContext(); var ids = await SeedAsync(db);
        var po = new PurchaseOrder("PO", ids.Supplier, ids.Warehouse, Actor, Now);
        po.AddLine(ids.Product, 1m, 0m, Now); db.Add(po); await db.SaveChangesAsync();
        var id = Guid.NewGuid();
        var error = await Assert.ThrowsAsync<SqlException>(() => db.Database.ExecuteSqlInterpolatedAsync(
            $"INSERT INTO [PurchaseOrderLines] ([Id],[PurchaseOrderId],[ProductId],[Quantity],[UnitPrice]) VALUES ({id},{po.Id},{ids.Product},{1m},{0m})"));
        Assert.True(error.Number == 2601 || error.Number == 2627);
    }

    [Fact]
    public async Task Missing_reference_and_deleting_referenced_product_are_rejected()
    {
        await using var database = new SqlTestDatabase(); await database.InitializeAsync();
        await using var db = database.CreateContext(); var ids = await SeedAsync(db);
        var po = new PurchaseOrder("PO", ids.Supplier, ids.Warehouse, Actor, Now);
        po.AddLine(ids.Product, 1m, 0m, Now); db.Add(po); await db.SaveChangesAsync();
        var sql = await Assert.ThrowsAsync<SqlException>(() => db.Database.ExecuteSqlInterpolatedAsync(
            $"DELETE FROM [Products] WHERE [Id] = {ids.Product}"));
        Assert.Equal(547, sql.Number);
        db.PurchaseOrders.Add(new PurchaseOrder("BAD", Guid.NewGuid(), ids.Warehouse, Actor, Now));
        AssertConstraint(await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync()));
    }

    [Fact]
    public async Task Balance_pair_is_unique_and_negative_quantity_is_rejected()
    {
        await using var database = new SqlTestDatabase(); await database.InitializeAsync();
        await using var db = database.CreateContext(); var ids = await SeedAsync(db);
        db.Add(new StockBalance(ids.Warehouse, ids.Product)); await db.SaveChangesAsync();
        var negative = await Assert.ThrowsAsync<SqlException>(() => db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE [StockBalances] SET [Quantity] = {-1m} WHERE [WarehouseId] = {ids.Warehouse} AND [ProductId] = {ids.Product}"));
        Assert.Equal(547, negative.Number);
        await using var other = database.CreateContext();
        other.Add(new StockBalance(ids.Warehouse, ids.Product));
        AssertUnique(await Assert.ThrowsAsync<DbUpdateException>(() => other.SaveChangesAsync()));
    }

    [Fact]
    public async Task SQL_rejects_same_warehouse_transfer_and_zero_line_quantity()
    {
        await using var database = new SqlTestDatabase(); await database.InitializeAsync();
        await using var db = database.CreateContext(); var ids = await SeedAsync(db);
        var transfer = new StockTransfer("TR", ids.Warehouse, ids.OtherWarehouse, Actor, Now);
        transfer.AddLine(ids.Product, 1m, Now); db.Add(transfer); await db.SaveChangesAsync();
        var same = await Assert.ThrowsAsync<SqlException>(() => db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE [StockTransfers] SET [ToWarehouseId] = {ids.Warehouse} WHERE [Id] = {transfer.Id}"));
        Assert.Equal(547, same.Number);
        var zero = await Assert.ThrowsAsync<SqlException>(() => db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE [StockTransferLines] SET [Quantity] = {0m} WHERE [StockTransferId] = {transfer.Id}"));
        Assert.Equal(547, zero.Number);
    }

    [Fact]
    public async Task Rowversion_rejects_stale_document_and_balance_updates()
    {
        await using var database = new SqlTestDatabase(); await database.InitializeAsync();
        Guid poId; Guid warehouse; Guid product;
        await using (var setup = database.CreateContext())
        {
            var ids = await SeedAsync(setup); warehouse = ids.Warehouse; product = ids.Product;
            var po = new PurchaseOrder("PO", ids.Supplier, warehouse, Actor, Now); poId = po.Id;
            setup.AddRange(po, new StockBalance(warehouse, product)); await setup.SaveChangesAsync();
        }
        await using var first = database.CreateContext(); await using var second = database.CreateContext();
        var a = await first.PurchaseOrders.SingleAsync(x => x.Id == poId);
        var b = await second.PurchaseOrders.SingleAsync(x => x.Id == poId);
        a.UpdateNotes("first", Now.AddMinutes(1)); b.UpdateNotes("second", Now.AddMinutes(2));
        await first.SaveChangesAsync();
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => second.SaveChangesAsync());
        await using var third = database.CreateContext(); await using var fourth = database.CreateContext();
        var c = await third.StockBalances.SingleAsync(); var d = await fourth.StockBalances.SingleAsync();
        // Explicit property writes are used only to exercise the provider's concurrency behavior.
        third.Entry(c).Property(x => x.Quantity).CurrentValue = 1m;
        fourth.Entry(d).Property(x => x.Quantity).CurrentValue = 2m;
        await third.SaveChangesAsync();
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => fourth.SaveChangesAsync());
    }

    [Fact]
    public async Task Rollback_restores_balance_and_removes_movement()
    {
        await using var database = new SqlTestDatabase(); await database.InitializeAsync();
        await using var db = database.CreateContext(); var ids = await SeedAsync(db);
        db.Add(new StockBalance(ids.Warehouse, ids.Product)); await db.SaveChangesAsync();
        await using (var tx = await db.Database.BeginTransactionAsync())
        {
            await db.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE [StockBalances] SET [Quantity] = {5m} WHERE [WarehouseId] = {ids.Warehouse} AND [ProductId] = {ids.Product}");
            db.Add(new StockMovement(StockDocumentType.Opening, Guid.NewGuid(), Guid.NewGuid(), ids.Warehouse, ids.Product, 5m, Actor, Now));
            await db.SaveChangesAsync();
            await tx.RollbackAsync();
        }
        await using var read = database.CreateContext();
        Assert.Equal(0m, (await read.StockBalances.SingleAsync()).Quantity);
        Assert.Empty(await read.StockMovements.ToListAsync());
    }

    [Fact]
    public async Task Ledger_source_is_unique_and_context_rejects_changes()
    {
        await using var database = new SqlTestDatabase(); await database.InitializeAsync();
        await using var db = database.CreateContext(); var ids = await SeedAsync(db);
        var document = Guid.NewGuid(); var line = Guid.NewGuid();
        var movement = new StockMovement(StockDocumentType.Opening, document, line, ids.Warehouse, ids.Product, 1m, Actor, Now);
        db.Add(movement); await db.SaveChangesAsync();
        db.Entry(movement).Property(x => x.SignedQuantity).CurrentValue = 2m;
        await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync());
        db.Entry(movement).State = EntityState.Unchanged;
        db.Remove(movement);
        await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync());
        db.Entry(movement).State = EntityState.Unchanged;
        db.Add(new StockMovement(StockDocumentType.Opening, document, line, ids.Warehouse, ids.Product, 1m, Actor, Now));
        AssertUnique(await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync()));
    }

    [Fact]
    public async Task Sequence_allocates_distinct_numbers_across_contexts()
    {
        await using var database = new SqlTestDatabase(); await database.InitializeAsync();
        await using var one = database.CreateContext(); await using var two = database.CreateContext();
        var numbers = await Task.WhenAll(new DocumentNumberGenerator(one).NextPurchaseOrderAsync(),
            new DocumentNumberGenerator(two).NextPurchaseOrderAsync());
        Assert.NotEqual(numbers[0], numbers[1]);
        Assert.All(numbers, value => Assert.StartsWith("PO-", value));
    }
}

using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using stockFlow.Domain.Entities;
using stockFlow.Infrastructure.Persistence;
using Xunit;

namespace stockFlow.IntegrationTests.Persistence;

// SQL provider metadata only: these tests do not open a database connection.
[Trait("Category", "Model")]
public class DocumentModelTests
{
    [Fact]
    public void Model_has_all_entities_precisions_tokens_and_restrictive_relationships()
    {
        using var context = new StockFlowDbContext(new DbContextOptionsBuilder<StockFlowDbContext>()
            .UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=MetadataOnly;Integrated Security=True").Options);
        var model = context.Model;
        Assert.Equal(22, model.GetEntityTypes().Count());
        foreach (var type in new[] { typeof(PurchaseOrder), typeof(StockOpening), typeof(StockReceipt), typeof(StockIssue), typeof(StockTransfer), typeof(StockBalance) })
        {
            var token = model.FindEntityType(type)!.FindProperty("RowVersion")!;
            Assert.True(token.IsConcurrencyToken);
        }
        var balance = model.FindEntityType(typeof(StockBalance))!;
        Assert.Equal(new[] { "WarehouseId", "ProductId" }, balance.FindPrimaryKey()!.Properties.Select(x => x.Name).ToArray());
        Assert.Equal(18, balance.FindProperty("Quantity")!.GetPrecision());
        Assert.Equal(3, balance.FindProperty("Quantity")!.GetScale());
        var order = model.FindEntityType(typeof(PurchaseOrder))!;
        Assert.Equal("_lines", order.FindNavigation("Lines")!.FieldInfo!.Name);
        Assert.Null(order.FindProperty("Total"));
        Assert.Null(model.FindEntityType(typeof(PurchaseOrderLine))!.FindProperty("LineTotal"));
        Assert.All(model.GetEntityTypes().SelectMany(x => x.GetForeignKeys()), fk => Assert.Equal(DeleteBehavior.Restrict, fk.DeleteBehavior));
        Assert.Contains(model.FindEntityType(typeof(StockReceipt))!.GetIndexes(), index =>
            index.IsUnique && index.Properties.Select(x => x.Name).SequenceEqual(new[] { "PurchaseOrderId" }));
    }
}

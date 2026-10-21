using System;
using System.Linq;
using stockFlow.Domain.Entities;
using stockFlow.Domain.Enums;
using Xunit;
namespace stockFlow.UnitTests.Domain;

public class DocumentTests
{
    private static readonly DateTime Now = new(2026, 10, 21, 10, 0, 0, DateTimeKind.Utc);
    private static Guid Id() => Guid.NewGuid();
    private static PurchaseOrder Order() => new("PO-TEST-001", Id(), Id(), Id(), Now);

    [Fact]
    public void Empty_order_cannot_be_submitted()
    {
        var order = Order();
        Assert.Throws<InvalidOperationException>(() => order.Submit(Id(), Now));
        Assert.Equal(PurchaseOrderStatus.Draft, order.Status);
    }
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(1.0001)]
    public void Invalid_quantities_are_rejected(decimal quantity)
    {
        var order = Order();
        Assert.Throws<ArgumentOutOfRangeException>(() => order.AddLine(Id(), quantity, 10m, Now));
        Assert.Empty(order.Lines);
    }
    [Theory]
    [InlineData(-1)]
    [InlineData(0.001)]
    public void Invalid_prices_are_rejected(decimal price)
    {
        var order = Order();
        Assert.Throws<ArgumentOutOfRangeException>(() => order.AddLine(Id(), 1m, price, Now));
        Assert.Empty(order.Lines);
    }
    [Fact]
    public void Duplicate_product_is_rejected_without_changing_order()
    {
        var order = Order(); var product = Id();
        order.AddLine(product, 1m, 10m, Now);
        Assert.Throws<InvalidOperationException>(() => order.AddLine(product, 2m, 10m, Now));
        Assert.Single(order.Lines);
        Assert.Equal(10m, order.Total);
    }
    [Fact]
    public void Totals_round_each_line_away_from_zero()
    {
        var order = Order();
        order.AddLine(Id(), 0.005m, 1m, Now);
        order.AddLine(Id(), 0.005m, 1m, Now);
        Assert.Equal(0.02m, order.Total);
    }
    [Fact]
    public void Submitted_order_is_frozen_and_can_be_received_once()
    {
        var order = Order(); order.AddLine(Id(), 2m, 10m, Now);
        order.Submit(Id(), Now);
        Assert.Throws<InvalidOperationException>(() => order.AddLine(Id(), 1m, 0m, Now));
        Assert.Throws<InvalidOperationException>(() => order.RemoveLine(order.Lines.Single().Id, Now));
        Assert.Throws<InvalidOperationException>(() => order.UpdateNotes("change", Now));
        order.MarkReceived(Id(), Now);
        Assert.Throws<InvalidOperationException>(() => order.MarkReceived(Id(), Now));
        Assert.Throws<InvalidOperationException>(() => order.Cancel(Id(), Now));
    }
    [Fact]
    public void Cancelled_order_cannot_be_submitted()
    {
        var order = Order(); order.Cancel(Id(), Now);
        Assert.Throws<InvalidOperationException>(() => order.Submit(Id(), Now));
    }
    [Fact]
    public void Receipt_copies_all_order_lines_and_prices()
    {
        var order = Order(); order.AddLine(Id(), 2m, 10m, Now); order.AddLine(Id(), 3m, 0m, Now);
        Assert.Throws<InvalidOperationException>(() => StockReceipt.FromPurchaseOrder("R", order, Id(), Now));
        order.Submit(Id(), Now);
        var receipt = StockReceipt.FromPurchaseOrder("R", order, Id(), Now);
        Assert.Equal(order.Id, receipt.PurchaseOrderId);
        Assert.Equal(order.WarehouseId, receipt.WarehouseId);
        Assert.Equal(order.Lines.Count, receipt.Lines.Count);
        foreach (var source in order.Lines)
        {
            var copy = receipt.Lines.Single(x => x.PurchaseOrderLineId == source.Id);
            Assert.Equal(source.ProductId, copy.ProductId);
            Assert.Equal(source.Quantity, copy.Quantity);
            Assert.Equal(source.UnitPrice, copy.UnitPrice);
        }
        receipt.Post(Id(), Now);
        Assert.Throws<InvalidOperationException>(() => receipt.Post(Id(), Now));
        Assert.Throws<InvalidOperationException>(() => receipt.Cancel(Id(), Now));
    }
    [Fact]
    public void Transfer_requires_different_warehouses()
    {
        var warehouse = Id();
        Assert.Throws<ArgumentException>(() => new StockTransfer("T", warehouse, warehouse, Id(), Now));
    }
    [Fact]
    public void Opening_requires_lines_and_is_immutable_after_posting()
    {
        var doc = new StockOpening("O", Id(), Id(), Now);
        Assert.Throws<InvalidOperationException>(() => doc.Post(Id(), Now));
        doc.AddLine(Id(), 2m, Now); doc.Post(Id(), Now);
        Assert.Throws<InvalidOperationException>(() => doc.AddLine(Id(), 1m, Now));
        Assert.Throws<InvalidOperationException>(() => doc.RemoveLine(doc.Lines.Single().Id, Now));
        Assert.Throws<InvalidOperationException>(() => doc.UpdateNotes("change", Now));
        Assert.Throws<InvalidOperationException>(() => doc.Cancel(Id(), Now));
    }
    [Fact]
    public void Issue_requires_reason_and_cancelled_issue_cannot_post()
    {
        Assert.Throws<ArgumentException>(() => new StockIssue("I", Id(), " ", Id(), Now));
        var issue = new StockIssue("I", Id(), "Damaged stock", Id(), Now);
        issue.AddLine(Id(), 1m, Now); issue.Cancel(Id(), Now);
        Assert.Throws<InvalidOperationException>(() => issue.Post(Id(), Now));
    }
    [Fact]
    public void Invalid_actor_and_backwards_time_do_not_change_status()
    {
        var order = Order(); order.AddLine(Id(), 1m, 0m, Now.AddMinutes(1));
        Assert.Throws<ArgumentException>(() => order.Submit(Guid.Empty, Now.AddMinutes(1)));
        Assert.Throws<ArgumentException>(() => order.Submit(Id(), Now));
        Assert.Equal(PurchaseOrderStatus.Draft, order.Status);
    }
    [Fact]
    public void Balance_starts_at_zero()
    {
        var balance = new StockBalance(Id(), Id());
        Assert.Equal(0m, balance.Quantity);
    }
    [Fact]
    public void Ledger_rejects_wrong_sign_and_zero_quantity()
    {
        Assert.Throws<ArgumentException>(() => new StockMovement(StockDocumentType.Issue, Id(), Id(), Id(), Id(), 1m, Id(), Now));
        Assert.Throws<ArgumentException>(() => new StockMovement(StockDocumentType.Receipt, Id(), Id(), Id(), Id(), -1m, Id(), Now));
        Assert.Throws<ArgumentOutOfRangeException>(() => new StockMovement(StockDocumentType.Transfer, Id(), Id(), Id(), Id(), 0m, Id(), Now));
        var movement = new StockMovement(StockDocumentType.Issue, Id(), Id(), Id(), Id(), -2m, Id(), Now);
        Assert.Equal(-2m, movement.SignedQuantity);
    }
    [Fact]
    public void Transfer_rejects_empty_post_duplicate_lines_and_reposting()
    {
        var transfer = new StockTransfer("T", Id(), Id(), Id(), Now);
        Assert.Throws<InvalidOperationException>(() => transfer.Post(Id(), Now));
        var product = Id();
        transfer.AddLine(product, 2m, Now);
        Assert.Throws<InvalidOperationException>(() => transfer.AddLine(product, 1m, Now));
        transfer.Post(Id(), Now);
        Assert.Throws<InvalidOperationException>(() => transfer.Post(Id(), Now));
        Assert.Throws<InvalidOperationException>(() => transfer.UpdateNotes("change", Now));
        Assert.Throws<InvalidOperationException>(() => transfer.Cancel(Id(), Now));
    }
    [Fact]
    public void Opening_rejects_duplicate_products_and_cancelled_edits()
    {
        var opening = new StockOpening("O", Id(), Id(), Now);
        var product = Id(); opening.AddLine(product, 1m, Now);
        Assert.Throws<InvalidOperationException>(() => opening.AddLine(product, 1m, Now));
        opening.Cancel(Id(), Now);
        Assert.Throws<InvalidOperationException>(() => opening.RemoveLine(opening.Lines.Single().Id, Now));
        Assert.Throws<InvalidOperationException>(() => opening.Post(Id(), Now));
    }
    [Fact]
    public void Invalid_utc_and_overflow_leave_no_partial_line()
    {
        Assert.Throws<ArgumentException>(() => new StockOpening("O", Id(), Id(), DateTime.SpecifyKind(Now, DateTimeKind.Unspecified)));
        var order = Order();
        Assert.Throws<OverflowException>(() => order.AddLine(Id(), 999999999999999.999m, 9999999999999999.99m, Now));
        Assert.Empty(order.Lines);
        Assert.Null(order.UpdatedAt);
    }
}

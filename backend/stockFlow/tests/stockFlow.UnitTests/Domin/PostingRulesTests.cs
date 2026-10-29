using stockFlow.Domain.Entities;
using Xunit;
namespace stockFlow.UnitTests.Domin;
public sealed class PostingRulesTests
{
    [Fact] public void Balance_applies_receipts_and_issues_and_preserves_value_on_failure()
    {
        var balance = new StockBalance(Guid.NewGuid(), Guid.NewGuid());
        balance.Apply(10m); balance.Apply(-3.125m); Assert.Equal(6.875m, balance.Quantity);
        Assert.Throws<InvalidOperationException>(() => balance.Apply(-7m)); Assert.Equal(6.875m, balance.Quantity);
    }
    [Theory] [InlineData(0)] [InlineData(0.0001)]
    public void Balance_rejects_zero_or_excess_scale(decimal delta)
    { var balance = new StockBalance(Guid.NewGuid(), Guid.NewGuid()); Assert.ThrowsAny<ArgumentException>(() => balance.Apply(delta)); }
    [Fact] public void Receipt_reopens_the_same_full_snapshot_but_posted_receipt_cannot_reopen()
    {
        var actor = Guid.NewGuid(); var now = DateTime.UtcNow;
        var order = new PurchaseOrder("PO-1", Guid.NewGuid(), Guid.NewGuid(), actor, now);
        order.AddLine(Guid.NewGuid(), 2.5m, 3m, now); order.Submit(actor, now);
        var receipt = StockReceipt.FromPurchaseOrder("RC-1", order, actor, now);
        var id = receipt.Id; var line = Assert.Single(receipt.Lines);
        receipt.Cancel(actor, now); receipt.Reopen(actor, now);
        Assert.Equal(id, receipt.Id); Assert.Equal(line.Id, Assert.Single(receipt.Lines).Id);
        Assert.Null(receipt.CancelledAt); receipt.Post(actor, now);
        Assert.Throws<InvalidOperationException>(() => receipt.Reopen(actor, now));
    }
}

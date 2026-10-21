using System;
using System.Collections.Generic;
using System.Linq;
using stockFlow.Domain.Common;
using stockFlow.Domain.Enums;

namespace stockFlow.Domain.Entities;

public sealed class StockReceiptLine
{
    public Guid Id { get; private set; }
    public Guid StockReceiptId { get; private set; }
    public Guid ProductId { get; private set; }
    public decimal Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal LineTotal => DocumentRules.LineTotal(Quantity, UnitPrice);
    public Guid PurchaseOrderLineId { get; private set; }
    private StockReceiptLine() { }
    internal StockReceiptLine(Guid parentId, Guid productId, decimal quantity, decimal unitPrice, Guid purchaseOrderLineId)
    {
        Id = Guid.NewGuid();
        StockReceiptId = DocumentRules.Id(parentId);
        ProductId = DocumentRules.Id(productId);
        Quantity = DocumentRules.Quantity(quantity);
        UnitPrice = DocumentRules.Price(unitPrice);
        _ = DocumentRules.LineTotal(Quantity, UnitPrice);
        PurchaseOrderLineId = DocumentRules.Id(purchaseOrderLineId);
    }
}

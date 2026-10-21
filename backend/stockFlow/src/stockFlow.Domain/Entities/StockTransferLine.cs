using System;
using System.Collections.Generic;
using System.Linq;
using stockFlow.Domain.Common;
using stockFlow.Domain.Enums;

namespace stockFlow.Domain.Entities;

public sealed class StockTransferLine
{
    public Guid Id { get; private set; }
    public Guid StockTransferId { get; private set; }
    public Guid ProductId { get; private set; }
    public decimal Quantity { get; private set; }
    private StockTransferLine() { }
    internal StockTransferLine(Guid parentId, Guid productId, decimal quantity)
    {
        Id = Guid.NewGuid();
        StockTransferId = DocumentRules.Id(parentId);
        ProductId = DocumentRules.Id(productId);
        Quantity = DocumentRules.Quantity(quantity);
    }
}

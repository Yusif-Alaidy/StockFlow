using System;
using System.Collections.Generic;
using System.Linq;
using stockFlow.Domain.Common;
using stockFlow.Domain.Enums;

namespace stockFlow.Domain.Entities;

public sealed class StockOpeningLine
{
    public Guid Id { get; private set; }
    public Guid StockOpeningId { get; private set; }
    public Guid ProductId { get; private set; }
    public decimal Quantity { get; private set; }
    private StockOpeningLine() { }
    internal StockOpeningLine(Guid parentId, Guid productId, decimal quantity)
    {
        Id = Guid.NewGuid();
        StockOpeningId = DocumentRules.Id(parentId);
        ProductId = DocumentRules.Id(productId);
        Quantity = DocumentRules.Quantity(quantity);
    }
}

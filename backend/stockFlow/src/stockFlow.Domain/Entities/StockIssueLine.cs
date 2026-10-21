using System;
using System.Collections.Generic;
using System.Linq;
using stockFlow.Domain.Common;
using stockFlow.Domain.Enums;

namespace stockFlow.Domain.Entities;

public sealed class StockIssueLine
{
    public Guid Id { get; private set; }
    public Guid StockIssueId { get; private set; }
    public Guid ProductId { get; private set; }
    public decimal Quantity { get; private set; }
    private StockIssueLine() { }
    internal StockIssueLine(Guid parentId, Guid productId, decimal quantity)
    {
        Id = Guid.NewGuid();
        StockIssueId = DocumentRules.Id(parentId);
        ProductId = DocumentRules.Id(productId);
        Quantity = DocumentRules.Quantity(quantity);
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using stockFlow.Domain.Common;
using stockFlow.Domain.Enums;

namespace stockFlow.Domain.Entities;

public sealed class StockBalance
{
    public Guid WarehouseId { get; private set; }
    public Guid ProductId { get; private set; }
    public decimal Quantity { get; private set; }
    public byte[] RowVersion { get; private set; } = Array.Empty<byte>();
    private StockBalance() { }
    public StockBalance(Guid warehouseId, Guid productId)
    {
        WarehouseId = DocumentRules.Id(warehouseId);
        ProductId = DocumentRules.Id(productId);
    }
    public void Apply(decimal delta)
    {
        if (delta == decimal.MinValue) throw new ArgumentOutOfRangeException(nameof(delta));
        DocumentRules.Quantity(Math.Abs(delta));
        var next = Quantity + delta;
        if (next < 0) throw new InvalidOperationException("Insufficient stock.");
        if (next > 999999999999999.999m) throw new ArgumentOutOfRangeException(nameof(delta));
        Quantity = next;
    }
}

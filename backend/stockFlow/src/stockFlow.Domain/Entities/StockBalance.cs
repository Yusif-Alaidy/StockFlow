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
    // No quantity mutator in Phase 03. Added with the transactional posting engine.
}

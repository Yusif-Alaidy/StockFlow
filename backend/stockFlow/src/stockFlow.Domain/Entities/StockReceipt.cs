using System;
using System.Collections.Generic;
using System.Linq;
using stockFlow.Domain.Common;
using stockFlow.Domain.Enums;

namespace stockFlow.Domain.Entities;

public sealed class StockReceipt
{
    public Guid Id { get; private set; }
    public string Number { get; private set; } = null!;
    public string? Notes { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public Guid CreatedBy { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public DateTime? CancelledAt { get; private set; }
    public Guid? CancelledBy { get; private set; }
    public byte[] RowVersion { get; private set; } = Array.Empty<byte>();
    public Guid WarehouseId { get; private set; }
    public Guid PurchaseOrderId { get; private set; }
    public DateTime? PostedAt { get; private set; }
    public Guid? PostedBy { get; private set; }
    public StockDocumentStatus Status { get; private set; } = StockDocumentStatus.Draft;
    private readonly List<StockReceiptLine> _lines = new();
    public IReadOnlyCollection<StockReceiptLine> Lines => _lines.AsReadOnly();
    private StockReceipt() { }
    private StockReceipt(string number, Guid warehouseId, Guid purchaseOrderId, Guid actorUserId, DateTime utcNow, string? notes = null)
    {
        Id = Guid.NewGuid();
        Number = DocumentRules.Text(number, 50);
        WarehouseId = DocumentRules.Id(warehouseId);
        PurchaseOrderId = DocumentRules.Id(purchaseOrderId);
        CreatedBy = DocumentRules.Id(actorUserId);
        CreatedAt = DocumentRules.Utc(utcNow);
        Notes = DocumentRules.Notes(notes);
    }
    private void EnsureDraft()
    {
        if (Status != StockDocumentStatus.Draft) throw new InvalidOperationException("Only draft documents can be edited.");
    }
    public void UpdateNotes(string? notes, DateTime utcNow)
    {
        EnsureDraft();
        var value = DocumentRules.Notes(notes);
        DocumentRules.Time(utcNow, UpdatedAt ?? CreatedAt);
        Notes = value;
        UpdatedAt = utcNow;
    }
    // Receipt lines are a complete snapshot; no manual add/remove in the MVP.
    public static StockReceipt FromPurchaseOrder(string number, PurchaseOrder order,
        Guid actorUserId, DateTime utcNow, string? notes = null)
    {
        if (order is null) throw new ArgumentNullException(nameof(order));
        if (order.Status != PurchaseOrderStatus.Submitted)
            throw new InvalidOperationException("Only submitted orders can be received.");
        DocumentRules.Time(utcNow, order.UpdatedAt ?? order.CreatedAt);
        var receipt = new StockReceipt(number, order.WarehouseId, order.Id, actorUserId, utcNow, notes);
        foreach (var line in order.Lines)
            receipt._lines.Add(new StockReceiptLine(receipt.Id, line.ProductId, line.Quantity, line.UnitPrice, line.Id));
        return receipt;
    }
    // State transition only; the future posting service owns SQL transactions and stock checks.
    public void Post(Guid actorUserId, DateTime utcNow)
    {
        EnsureDraft();
        if (_lines.Count == 0) throw new InvalidOperationException("An empty document cannot be posted.");
        DocumentRules.Id(actorUserId);
        DocumentRules.Time(utcNow, UpdatedAt ?? CreatedAt);
        Status = StockDocumentStatus.Posted;
        PostedBy = actorUserId;
        PostedAt = UpdatedAt = utcNow;
    }
    public void Cancel(Guid actorUserId, DateTime utcNow)
    {
        EnsureDraft();
        DocumentRules.Id(actorUserId);
        DocumentRules.Time(utcNow, UpdatedAt ?? CreatedAt);
        Status = StockDocumentStatus.Cancelled;
        CancelledBy = actorUserId;
        CancelledAt = UpdatedAt = utcNow;
    }
}

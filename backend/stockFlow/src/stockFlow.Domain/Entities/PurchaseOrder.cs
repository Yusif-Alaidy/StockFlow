using System;
using System.Collections.Generic;
using System.Linq;
using stockFlow.Domain.Common;
using stockFlow.Domain.Enums;

namespace stockFlow.Domain.Entities;

public sealed class PurchaseOrder
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
    public Guid SupplierId { get; private set; }
    public Guid WarehouseId { get; private set; }
    public DateTime? SubmittedAt { get; private set; }
    public Guid? SubmittedBy { get; private set; }
    public DateTime? ReceivedAt { get; private set; }
    public Guid? ReceivedBy { get; private set; }
    public decimal Total => _lines.Sum(x => x.LineTotal);
    public PurchaseOrderStatus Status { get; private set; } = PurchaseOrderStatus.Draft;
    private readonly List<PurchaseOrderLine> _lines = new();
    public IReadOnlyCollection<PurchaseOrderLine> Lines => _lines.AsReadOnly();
    private PurchaseOrder() { }
    public PurchaseOrder(string number, Guid supplierId, Guid warehouseId, Guid actorUserId, DateTime utcNow, string? notes = null)
    {
        Id = Guid.NewGuid();
        Number = DocumentRules.Text(number, 50);
        SupplierId = DocumentRules.Id(supplierId);
        WarehouseId = DocumentRules.Id(warehouseId);
        CreatedBy = DocumentRules.Id(actorUserId);
        CreatedAt = DocumentRules.Utc(utcNow);
        Notes = DocumentRules.Notes(notes);
    }
    private void EnsureDraft()
    {
        if (Status != PurchaseOrderStatus.Draft) throw new InvalidOperationException("Only draft documents can be edited.");
    }
    public void UpdateNotes(string? notes, DateTime utcNow)
    {
        EnsureDraft();
        var value = DocumentRules.Notes(notes);
        DocumentRules.Time(utcNow, UpdatedAt ?? CreatedAt);
        Notes = value;
        UpdatedAt = utcNow;
    }
    public void AddLine(Guid productId, decimal quantity, decimal unitPrice, DateTime utcNow)
    {
        EnsureDraft();
        DocumentRules.Time(utcNow, UpdatedAt ?? CreatedAt);
        if (_lines.Any(x => x.ProductId == productId)) throw new InvalidOperationException("Product already exists in this document.");
        var line = new PurchaseOrderLine(Id, productId, quantity, unitPrice);
        DocumentRules.Price(Total + line.LineTotal);
        _lines.Add(line);
        UpdatedAt = utcNow;
    }
    public void RemoveLine(Guid lineId, DateTime utcNow)
    {
        EnsureDraft();
        DocumentRules.Time(utcNow, UpdatedAt ?? CreatedAt);
        var line = _lines.SingleOrDefault(x => x.Id == lineId)
            ?? throw new InvalidOperationException("Line not found.");
        _lines.Remove(line);
        UpdatedAt = utcNow;
    }
    public void Submit(Guid actorUserId, DateTime utcNow)
    {
        EnsureDraft();
        if (_lines.Count == 0) throw new InvalidOperationException("An empty order cannot be submitted.");
        DocumentRules.Id(actorUserId);
        DocumentRules.Time(utcNow, UpdatedAt ?? CreatedAt);
        Status = PurchaseOrderStatus.Submitted;
        SubmittedBy = actorUserId;
        SubmittedAt = UpdatedAt = utcNow;
    }
    // Called by the posting use case in the same SQL transaction as the receipt and ledger.
    public void MarkReceived(Guid actorUserId, DateTime utcNow)
    {
        if (Status != PurchaseOrderStatus.Submitted) throw new InvalidOperationException("Only submitted orders can be received.");
        DocumentRules.Id(actorUserId);
        DocumentRules.Time(utcNow, UpdatedAt ?? CreatedAt);
        Status = PurchaseOrderStatus.Received;
        ReceivedBy = actorUserId;
        ReceivedAt = UpdatedAt = utcNow;
    }
    public void Cancel(Guid actorUserId, DateTime utcNow)
    {
        if (Status != PurchaseOrderStatus.Draft && Status != PurchaseOrderStatus.Submitted) throw new InvalidOperationException("Order cannot be cancelled.");
        DocumentRules.Id(actorUserId);
        DocumentRules.Time(utcNow, UpdatedAt ?? CreatedAt);
        Status = PurchaseOrderStatus.Cancelled;
        CancelledBy = actorUserId;
        CancelledAt = UpdatedAt = utcNow;
    }
}

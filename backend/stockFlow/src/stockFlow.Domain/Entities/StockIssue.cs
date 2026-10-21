using System;
using System.Collections.Generic;
using System.Linq;
using stockFlow.Domain.Common;
using stockFlow.Domain.Enums;

namespace stockFlow.Domain.Entities;

public sealed class StockIssue
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
    public string Reason { get; private set; } = null!;
    public DateTime? PostedAt { get; private set; }
    public Guid? PostedBy { get; private set; }
    public StockDocumentStatus Status { get; private set; } = StockDocumentStatus.Draft;
    private readonly List<StockIssueLine> _lines = new();
    public IReadOnlyCollection<StockIssueLine> Lines => _lines.AsReadOnly();
    private StockIssue() { }
    public StockIssue(string number, Guid warehouseId, string reason, Guid actorUserId, DateTime utcNow, string? notes = null)
    {
        Id = Guid.NewGuid();
        Number = DocumentRules.Text(number, 50);
        WarehouseId = DocumentRules.Id(warehouseId);
        Reason = DocumentRules.Text(reason, 300);
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
    public void AddLine(Guid productId, decimal quantity, DateTime utcNow)
    {
        EnsureDraft();
        DocumentRules.Time(utcNow, UpdatedAt ?? CreatedAt);
        if (_lines.Any(x => x.ProductId == productId)) throw new InvalidOperationException("Product already exists in this document.");
        var line = new StockIssueLine(Id, productId, quantity);
        
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

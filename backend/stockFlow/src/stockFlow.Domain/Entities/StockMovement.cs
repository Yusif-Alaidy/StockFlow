using System;
using System.Collections.Generic;
using System.Linq;
using stockFlow.Domain.Common;
using stockFlow.Domain.Enums;

namespace stockFlow.Domain.Entities;

public sealed class StockMovement
{
    public Guid Id { get; private set; }
    public StockDocumentType DocumentType { get; private set; }
    public Guid DocumentId { get; private set; }
    public Guid DocumentLineId { get; private set; }
    public Guid WarehouseId { get; private set; }
    public Guid ProductId { get; private set; }
    public decimal SignedQuantity { get; private set; }
    public DateTime OccurredAt { get; private set; }
    public Guid ActorUserId { get; private set; }
    private StockMovement() { }
    public StockMovement(StockDocumentType documentType, Guid documentId, Guid documentLineId,
        Guid warehouseId, Guid productId, decimal signedQuantity, Guid actorUserId, DateTime utcNow)
    {
        if (!Enum.IsDefined(typeof(StockDocumentType), documentType)) throw new ArgumentException("Invalid document type.");
        if (signedQuantity == decimal.MinValue) throw new ArgumentOutOfRangeException(nameof(signedQuantity));
        DocumentRules.Quantity(Math.Abs(signedQuantity));
        if ((documentType == StockDocumentType.Opening || documentType == StockDocumentType.Receipt) && signedQuantity < 0)
            throw new ArgumentException("Inbound movement must be positive.");
        if (documentType == StockDocumentType.Issue && signedQuantity > 0)
            throw new ArgumentException("Issue movement must be negative.");
        Id = Guid.NewGuid();
        DocumentType = documentType;
        DocumentId = DocumentRules.Id(documentId);
        DocumentLineId = DocumentRules.Id(documentLineId);
        WarehouseId = DocumentRules.Id(warehouseId);
        ProductId = DocumentRules.Id(productId);
        SignedQuantity = signedQuantity;
        ActorUserId = DocumentRules.Id(actorUserId);
        OccurredAt = DocumentRules.Utc(utcNow);
    }
}

using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using stockFlow.Domain.Entities;
using stockFlow.Domain.Enums;

namespace stockFlow.Infrastructure.Persistence.Configurations;

public sealed class StockMovementConfiguration : IEntityTypeConfiguration<StockMovement>
{
    public void Configure(EntityTypeBuilder<StockMovement> builder)
    {
        builder.ToTable("StockMovements", table =>
        {
            table.HasCheckConstraint("CK_StockMovements_Type", "[DocumentType] BETWEEN 0 AND 3");
            table.HasCheckConstraint("CK_StockMovements_Sign",
                "([DocumentType] IN (0, 1) AND [SignedQuantity] > 0) OR " +
                "([DocumentType] = 2 AND [SignedQuantity] < 0) OR " +
                "([DocumentType] = 3 AND [SignedQuantity] <> 0)");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.DocumentType).HasConversion<int>();
        builder.Property(x => x.SignedQuantity).HasPrecision(18, 3);
        builder.HasIndex(x => new { x.DocumentType, x.DocumentId, x.DocumentLineId, x.WarehouseId }).IsUnique();
        builder.HasIndex(x => new { x.WarehouseId, x.ProductId, x.OccurredAt });
        builder.Property(x => x.OccurredAt).HasColumnType("datetime2").HasConversion(value => value, value => DateTime.SpecifyKind(value, DateTimeKind.Utc));
        builder.HasOne<Warehouse>().WithMany().HasForeignKey(x => x.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Product>().WithMany().HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

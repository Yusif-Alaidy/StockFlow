using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using stockFlow.Domain.Entities;
using stockFlow.Domain.Enums;

namespace stockFlow.Infrastructure.Persistence.Configurations;

public sealed class StockReceiptLineConfiguration : IEntityTypeConfiguration<StockReceiptLine>
{
    public void Configure(EntityTypeBuilder<StockReceiptLine> builder)
    {
        builder.ToTable("StockReceiptLines", table =>
        {
            table.HasCheckConstraint("CK_StockReceiptLines_Quantity", "[Quantity] > 0");
            table.HasCheckConstraint("CK_StockReceiptLines_UnitPrice", "[UnitPrice] >= 0");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Quantity).HasPrecision(18, 3);
        builder.HasIndex(x => new { x.StockReceiptId, x.ProductId }).IsUnique();
        builder.HasOne<Product>().WithMany().HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Property(x => x.UnitPrice).HasPrecision(18, 2);
        builder.Ignore(x => x.LineTotal);
        builder.HasOne<PurchaseOrderLine>().WithMany().HasForeignKey(x => x.PurchaseOrderLineId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => x.PurchaseOrderLineId).IsUnique();
    }
}

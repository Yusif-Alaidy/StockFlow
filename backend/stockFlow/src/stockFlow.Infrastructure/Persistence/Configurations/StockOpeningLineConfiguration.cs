using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using stockFlow.Domain.Entities;
using stockFlow.Domain.Enums;

namespace stockFlow.Infrastructure.Persistence.Configurations;

public sealed class StockOpeningLineConfiguration : IEntityTypeConfiguration<StockOpeningLine>
{
    public void Configure(EntityTypeBuilder<StockOpeningLine> builder)
    {
        builder.ToTable("StockOpeningLines", table =>
        {
            table.HasCheckConstraint("CK_StockOpeningLines_Quantity", "[Quantity] > 0");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Quantity).HasPrecision(18, 3);
        builder.HasIndex(x => new { x.StockOpeningId, x.ProductId }).IsUnique();
        builder.HasOne<Product>().WithMany().HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

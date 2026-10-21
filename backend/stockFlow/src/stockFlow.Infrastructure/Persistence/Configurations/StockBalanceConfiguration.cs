using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using stockFlow.Domain.Entities;
using stockFlow.Domain.Enums;

namespace stockFlow.Infrastructure.Persistence.Configurations;

public sealed class StockBalanceConfiguration : IEntityTypeConfiguration<StockBalance>
{
    public void Configure(EntityTypeBuilder<StockBalance> builder)
    {
        builder.ToTable("StockBalances", table =>
            table.HasCheckConstraint("CK_StockBalances_Quantity", "[Quantity] >= 0"));
        builder.HasKey(x => new { x.WarehouseId, x.ProductId });
        builder.Property(x => x.Quantity).HasPrecision(18, 3);
        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.HasOne<Warehouse>().WithMany().HasForeignKey(x => x.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Product>().WithMany().HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

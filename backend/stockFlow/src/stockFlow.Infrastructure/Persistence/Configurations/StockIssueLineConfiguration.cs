using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using stockFlow.Domain.Entities;
using stockFlow.Domain.Enums;

namespace stockFlow.Infrastructure.Persistence.Configurations;

public sealed class StockIssueLineConfiguration : IEntityTypeConfiguration<StockIssueLine>
{
    public void Configure(EntityTypeBuilder<StockIssueLine> builder)
    {
        builder.ToTable("StockIssueLines", table =>
        {
            table.HasCheckConstraint("CK_StockIssueLines_Quantity", "[Quantity] > 0");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Quantity).HasPrecision(18, 3);
        builder.HasIndex(x => new { x.StockIssueId, x.ProductId }).IsUnique();
        builder.HasOne<Product>().WithMany().HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

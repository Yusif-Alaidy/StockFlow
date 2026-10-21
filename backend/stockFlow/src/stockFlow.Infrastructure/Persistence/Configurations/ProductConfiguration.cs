using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using stockFlow.Domain.Common;
using stockFlow.Domain.Entities;

namespace stockFlow.Infrastructure.Persistence.Configurations;

public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ConfigureMaster("Products");
        builder.Property(entity => entity.Sku)
            .HasMaxLength(FieldLimits.Sku).IsRequired();
        builder.HasIndex(entity => entity.Sku).IsUnique();
        builder.Property(entity => entity.Name)
            .HasMaxLength(FieldLimits.ProductName).IsRequired();

        builder.HasOne<Category>()
            .WithMany()
            .HasForeignKey(entity => entity.CategoryId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Unit>()
            .WithMany()
            .HasForeignKey(entity => entity.UnitId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);
    }
}

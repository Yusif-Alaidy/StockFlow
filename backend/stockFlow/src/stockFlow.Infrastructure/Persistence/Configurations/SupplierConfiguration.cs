using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using stockFlow.Domain.Common;
using stockFlow.Domain.Entities;

namespace stockFlow.Infrastructure.Persistence.Configurations;

public sealed class SupplierConfiguration : IEntityTypeConfiguration<Supplier>
{
    public void Configure(EntityTypeBuilder<Supplier> builder)
    {
        builder.ConfigureMaster("Suppliers");
        builder.Property(entity => entity.Code)
            .HasMaxLength(FieldLimits.Code).IsRequired();
        builder.HasIndex(entity => entity.Code).IsUnique();
        builder.Property(entity => entity.Name)
            .HasMaxLength(FieldLimits.SupplierName).IsRequired();

        builder.Property(entity => entity.Email).HasMaxLength(FieldLimits.Email);
        builder.Property(entity => entity.Phone).HasMaxLength(FieldLimits.Phone);
        builder.Property(entity => entity.Address).HasMaxLength(FieldLimits.Address);
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using stockFlow.Domain.Common;
using stockFlow.Domain.Entities;

namespace stockFlow.Infrastructure.Persistence.Configurations;

public sealed class WarehouseConfiguration : IEntityTypeConfiguration<Warehouse>
{
    public void Configure(EntityTypeBuilder<Warehouse> builder)
    {
        builder.ConfigureMaster("Warehouses");
        builder.Property(entity => entity.Code)
            .HasMaxLength(FieldLimits.Code).IsRequired();
        builder.HasIndex(entity => entity.Code).IsUnique();
        builder.Property(entity => entity.Name)
            .HasMaxLength(FieldLimits.Name).IsRequired();

        builder.HasOne<Branch>()
            .WithMany()
            .HasForeignKey(entity => entity.BranchId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);
    }
}

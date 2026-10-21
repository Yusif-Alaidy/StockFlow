using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using stockFlow.Domain.Common;
using stockFlow.Domain.Entities;

namespace stockFlow.Infrastructure.Persistence.Configurations;

public sealed class UnitConfiguration : IEntityTypeConfiguration<Unit>
{
    public void Configure(EntityTypeBuilder<Unit> builder)
    {
        builder.ConfigureMaster("Units");
        builder.Property(entity => entity.Code)
            .HasMaxLength(FieldLimits.Code).IsRequired();
        builder.HasIndex(entity => entity.Code).IsUnique();
        builder.Property(entity => entity.Name)
            .HasMaxLength(FieldLimits.Name).IsRequired();
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using stockFlow.Domain.Common;
using stockFlow.Domain.Entities;

namespace stockFlow.Infrastructure.Persistence.Configurations;

public sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ConfigureMaster("Categories");
        builder.Property(entity => entity.Name)
            .HasMaxLength(FieldLimits.Name).IsRequired();
    }
}

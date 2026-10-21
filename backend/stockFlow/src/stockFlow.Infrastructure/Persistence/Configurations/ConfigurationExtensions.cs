using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using stockFlow.Domain.Common;

namespace stockFlow.Infrastructure.Persistence.Configurations;

internal static class ConfigurationExtensions
{
    public static void ConfigureMaster<TEntity>(
        this EntityTypeBuilder<TEntity> builder, string tableName)
        where TEntity : BaseEntity
    {
        // Six independent tables, not a shared BaseEntity TPH table.
        builder.HasBaseType((Type?)null);
        builder.ToTable(tableName);
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).ValueGeneratedNever();
        builder.Property(entity => entity.IsActive).IsRequired();

        builder.Property(entity => entity.CreatedAt)
            .HasColumnType("datetime2")
            .IsRequired()
            .HasConversion(value => value,
                value => DateTime.SpecifyKind(value, DateTimeKind.Utc));

        builder.Property(entity => entity.UpdatedAt)
            .HasColumnType("datetime2")
            .HasConversion(value => value,
                value => value.HasValue
                    ? (DateTime?)DateTime.SpecifyKind(value.Value, DateTimeKind.Utc)
                    : null);

        // No global IsActive filter: historical references must remain visible.
        // RowVersion/ETag behavior will be added in the persistence/API phases.
    }
}

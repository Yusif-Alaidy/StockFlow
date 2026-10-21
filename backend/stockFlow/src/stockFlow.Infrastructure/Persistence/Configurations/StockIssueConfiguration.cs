using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using stockFlow.Domain.Entities;
using stockFlow.Domain.Enums;

namespace stockFlow.Infrastructure.Persistence.Configurations;

public sealed class StockIssueConfiguration : IEntityTypeConfiguration<StockIssue>
{
    public void Configure(EntityTypeBuilder<StockIssue> builder)
    {
        builder.ToTable("StockIssues", table =>
        {
            table.HasCheckConstraint("CK_StockIssues_Status", "[Status] BETWEEN 0 AND 2");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Number).HasMaxLength(50).IsRequired();
        builder.HasIndex(x => x.Number).IsUnique();
        builder.Property(x => x.Notes).HasMaxLength(1000);
        builder.Property(x => x.Status).HasConversion<int>().IsRequired();
        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.Property(x => x.CreatedAt).HasColumnType("datetime2").HasConversion(value => value, value => DateTime.SpecifyKind(value, DateTimeKind.Utc));
        builder.Property(x => x.UpdatedAt).HasColumnType("datetime2").HasConversion(value => value, value => value.HasValue ? (DateTime?)DateTime.SpecifyKind(value.Value, DateTimeKind.Utc) : null);
        builder.Property(x => x.CancelledAt).HasColumnType("datetime2").HasConversion(value => value, value => value.HasValue ? (DateTime?)DateTime.SpecifyKind(value.Value, DateTimeKind.Utc) : null);
        builder.Property(x => x.PostedAt).HasColumnType("datetime2").HasConversion(value => value, value => value.HasValue ? (DateTime?)DateTime.SpecifyKind(value.Value, DateTimeKind.Utc) : null);
        builder.HasOne<Warehouse>().WithMany().HasForeignKey(x => x.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Property(x => x.Reason).HasMaxLength(300).IsRequired();
        builder.HasMany(x => x.Lines).WithOne().HasForeignKey(x => x.StockIssueId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Navigation(x => x.Lines).HasField("_lines")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

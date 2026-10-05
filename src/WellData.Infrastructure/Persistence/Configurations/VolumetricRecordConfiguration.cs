using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WellData.Core.Entities;

namespace WellData.Infrastructure.Persistence.Configurations;

public sealed class VolumetricRecordConfiguration : IEntityTypeConfiguration<VolumetricRecord>
{
    public void Configure(EntityTypeBuilder<VolumetricRecord> builder)
    {
        // System-versioned: SQL Server keeps every earlier version of a row in VolumetricRecordsHistory,
        // so a restated month never silently overwrites what was loaded before.
        builder.ToTable("VolumetricRecords", table => table.IsTemporal());

        builder.Property(record => record.OperatorBaId).HasMaxLength(4).IsUnicode(false);
        builder.Property(record => record.ReportingFacilityId).HasMaxLength(20).IsUnicode(false);
        builder.Property(record => record.ActivityId).HasMaxLength(10).IsUnicode(false);
        builder.Property(record => record.ProductId).HasMaxLength(10).IsUnicode(false);
        builder.Property(record => record.FromToId).HasMaxLength(20).IsUnicode(false);
        builder.Property(record => record.FromToIdType).HasMaxLength(2).IsUnicode(false);
        builder.Property(record => record.Volume).HasPrecision(13, 3);
        builder.Property(record => record.Energy).HasPrecision(13, 3);

        // Facility and From/To IDs are deliberately not foreign keys. Petrinex volumes can reference
        // wells and facilities outside the loaded subset, which rule REF-01 reports instead of rejecting.
        builder
            .HasOne<ActivityCode>()
            .WithMany()
            .HasForeignKey(record => record.ActivityId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne<ProductCode>()
            .WithMany()
            .HasForeignKey(record => record.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(record => record.SourceFile)
            .WithMany()
            .HasForeignKey(record => record.SourceFileId)
            .OnDelete(DeleteBehavior.Restrict);

        // Covers the per-month rules (DUP-01, REF-01, RAT-01) so they never touch the base table.
        builder
            .HasIndex(record => new { record.ProductionMonth, record.ReportingFacilityId })
            .IncludeProperties(record => new
            {
                record.ActivityId,
                record.ProductId,
                record.FromToId,
                record.FromToIdType,
                record.Volume,
                record.IsMasked,
                record.SourceRowNumber,
            })
            .HasDatabaseName("IX_VolumetricRecords_Month_Facility");

        // Covers per-well history: the LAG and trailing-window rules, the monthly production view, and well lookups.
        builder
            .HasIndex(record => new { record.FromToId, record.ProductionMonth })
            .IncludeProperties(record => new
            {
                record.FromToIdType,
                record.ActivityId,
                record.ProductId,
                record.Volume,
                record.Hours,
                record.ReportingFacilityId,
                record.OperatorBaId,
                record.IsMasked,
            })
            .HasDatabaseName("IX_VolumetricRecords_Well_Month");
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WellData.Core.Entities;

namespace WellData.Infrastructure.Persistence.Configurations;

public sealed class StagedVolumetricRowConfiguration : IEntityTypeConfiguration<StagedVolumetricRow>
{
    public const int TextLength = 50;
    public const int NameLength = 150;

    public void Configure(EntityTypeBuilder<StagedVolumetricRow> builder)
    {
        builder.ToTable("StagingVolumetricRows");

        builder.Property(row => row.ProductionMonth).HasMaxLength(TextLength);
        builder.Property(row => row.OperatorBaId).HasMaxLength(TextLength);
        builder.Property(row => row.OperatorName).HasMaxLength(NameLength);
        builder.Property(row => row.ReportingFacilityId).HasMaxLength(TextLength);
        builder.Property(row => row.ReportingFacilityType).HasMaxLength(TextLength);
        builder.Property(row => row.ReportingFacilityName).HasMaxLength(NameLength);
        builder.Property(row => row.SubmissionDate).HasMaxLength(TextLength);
        builder.Property(row => row.ActivityId).HasMaxLength(TextLength);
        builder.Property(row => row.ProductId).HasMaxLength(TextLength);
        builder.Property(row => row.FromToId).HasMaxLength(TextLength);
        builder.Property(row => row.FromToIdType).HasMaxLength(TextLength);
        builder.Property(row => row.Volume).HasMaxLength(TextLength);
        builder.Property(row => row.Energy).HasMaxLength(TextLength);
        builder.Property(row => row.Hours).HasMaxLength(TextLength);
        builder.Property(row => row.ProrationProduct).HasMaxLength(TextLength);
        builder.Property(row => row.ProrationFactor).HasMaxLength(TextLength);

        builder
            .HasIndex(row => new { row.SourceFileId, row.RowNumber })
            .HasDatabaseName("IX_StagingVolumetricRows_SourceFile_RowNumber");

        builder
            .HasOne<SourceFile>()
            .WithMany()
            .HasForeignKey(row => row.SourceFileId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

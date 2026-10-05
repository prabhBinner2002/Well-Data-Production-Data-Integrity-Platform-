using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WellData.Core.Entities;

namespace WellData.Infrastructure.Persistence.Configurations;

public sealed class DataQualityIssueConfiguration : IEntityTypeConfiguration<DataQualityIssue>
{
    public void Configure(EntityTypeBuilder<DataQualityIssue> builder)
    {
        builder.Property(issue => issue.RuleCode).HasMaxLength(10).IsUnicode(false);
        builder.Property(issue => issue.FacilityId).HasMaxLength(50).IsUnicode(false);
        builder.Property(issue => issue.WellId).HasMaxLength(50).IsUnicode(false);
        builder.Property(issue => issue.Field).HasMaxLength(50).IsUnicode(false);
        builder.Property(issue => issue.RawValue).HasMaxLength(150);
        builder.Property(issue => issue.Message).HasMaxLength(500);
        builder.Property(issue => issue.Status).HasConversion<string>().HasMaxLength(15).IsUnicode(false);
        builder.Property(issue => issue.ResolutionNote).HasMaxLength(500);
        builder.Property(issue => issue.RowVersion).IsRowVersion();
        builder.ConfigureAuditFields();

        builder
            .HasOne(issue => issue.Rule)
            .WithMany()
            .HasForeignKey(issue => issue.RuleCode)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne<SourceFile>()
            .WithMany()
            .HasForeignKey(issue => issue.SourceFileId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasIndex(issue => new { issue.ProductionMonth, issue.RuleCode, issue.Status })
            .HasDatabaseName("IX_DataQualityIssues_Month_Rule_Status");
        builder.HasIndex(issue => issue.WellId).HasDatabaseName("IX_DataQualityIssues_WellId");
    }
}

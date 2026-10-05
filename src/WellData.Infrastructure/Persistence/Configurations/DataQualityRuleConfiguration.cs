using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WellData.Core.Entities;
using WellData.Core.Validation;

namespace WellData.Infrastructure.Persistence.Configurations;

public sealed class DataQualityRuleConfiguration : IEntityTypeConfiguration<DataQualityRule>
{
    public void Configure(EntityTypeBuilder<DataQualityRule> builder)
    {
        builder.HasKey(rule => rule.Code);
        builder.Property(rule => rule.Code).HasMaxLength(10).IsUnicode(false);
        builder.Property(rule => rule.Description).HasMaxLength(200);
        builder.Property(rule => rule.Severity).HasConversion<string>().HasMaxLength(10).IsUnicode(false);
        builder.Property(rule => rule.RowVersion).IsRowVersion();
        builder.ConfigureAuditFields();

        builder.HasData(RuleCatalog.FileRules);
        builder.HasData(RuleCatalog.SqlRules);
    }
}

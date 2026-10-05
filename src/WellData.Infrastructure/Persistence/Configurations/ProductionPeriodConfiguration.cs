using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WellData.Core.Entities;

namespace WellData.Infrastructure.Persistence.Configurations;

public sealed class ProductionPeriodConfiguration : IEntityTypeConfiguration<ProductionPeriod>
{
    public void Configure(EntityTypeBuilder<ProductionPeriod> builder)
    {
        builder.Property(period => period.Status).HasConversion<string>().HasMaxLength(10).IsUnicode(false);
        builder.Property(period => period.ClosedBy).HasMaxLength(100);
        builder.ConfigureAuditFields();

        // The unique index is what stops two stewards closing the same month at the same moment.
        builder.HasIndex(period => period.ProductionMonth).IsUnique().HasDatabaseName("UX_ProductionPeriods_ProductionMonth");
    }
}

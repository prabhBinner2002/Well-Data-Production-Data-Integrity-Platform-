using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WellData.Core.Entities;

namespace WellData.Infrastructure.Persistence.Configurations;

public sealed class ErpExportConfiguration : IEntityTypeConfiguration<ErpExport>
{
    public void Configure(EntityTypeBuilder<ErpExport> builder)
    {
        builder.Property(export => export.FileName).HasMaxLength(100);
        builder.Property(export => export.ExportedBy).HasMaxLength(100);
        builder.Property(export => export.OilVolume).HasPrecision(18, 3);
        builder.Property(export => export.GasVolume).HasPrecision(18, 3);
        builder.Property(export => export.WaterVolume).HasPrecision(18, 3);
        builder.Property(export => export.CondensateVolume).HasPrecision(18, 3);

        builder.HasIndex(export => export.ProductionMonth).HasDatabaseName("IX_ErpExports_ProductionMonth");
    }
}

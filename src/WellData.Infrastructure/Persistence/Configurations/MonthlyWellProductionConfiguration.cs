using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WellData.Core.Entities;

namespace WellData.Infrastructure.Persistence.Configurations;

public sealed class MonthlyWellProductionConfiguration : IEntityTypeConfiguration<MonthlyWellProduction>
{
    public const string ViewName = "vw_MonthlyWellProduction";

    public void Configure(EntityTypeBuilder<MonthlyWellProduction> builder)
    {
        builder.HasNoKey();
        builder.ToView(ViewName);

        // These must match the varchar columns under the view. Left as the default nvarchar, EF sends Unicode
        // parameters, SQL Server converts the column side to compare, and a filter by well can no longer
        // seek on IX_VolumetricRecords_Well_Month.
        builder.Property(row => row.WellId).HasMaxLength(20).IsUnicode(false);
        builder.Property(row => row.FacilityId).HasMaxLength(20).IsUnicode(false);
        builder.Property(row => row.OperatorBaId).HasMaxLength(4).IsUnicode(false);

        builder.Property(row => row.OilVolume).HasPrecision(38, 3);
        builder.Property(row => row.GasVolume).HasPrecision(38, 3);
        builder.Property(row => row.WaterVolume).HasPrecision(38, 3);
        builder.Property(row => row.CondensateVolume).HasPrecision(38, 3);
    }
}

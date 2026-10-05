using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WellData.Core.Entities;

namespace WellData.Infrastructure.Persistence.Configurations;

public sealed class WellConfiguration : IEntityTypeConfiguration<Well>
{
    public void Configure(EntityTypeBuilder<Well> builder)
    {
        builder.Property(well => well.WellId).HasMaxLength(20).IsUnicode(false);
        builder.Property(well => well.Uwi).HasMaxLength(24).IsUnicode(false);
        builder.Property(well => well.Name).HasMaxLength(100);
        builder.Property(well => well.LicenceNumber).HasMaxLength(10).IsUnicode(false);
        builder.Property(well => well.LicenseeBaId).HasMaxLength(4).IsUnicode(false);
        builder.Property(well => well.Field).HasMaxLength(50);
        builder.Property(well => well.StatusFluid).HasMaxLength(20);
        builder.Property(well => well.StatusMode).HasMaxLength(20);
        builder.Property(well => well.LicenceStatus).HasMaxLength(20);

        builder.HasIndex(well => well.WellId).IsUnique().HasDatabaseName("UX_Wells_WellId");
        builder.HasIndex(well => well.LicenseeBaId).HasDatabaseName("IX_Wells_LicenseeBaId");
    }
}

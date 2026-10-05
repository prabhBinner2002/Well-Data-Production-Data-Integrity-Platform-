using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WellData.Core.Entities;

namespace WellData.Infrastructure.Persistence.Configurations;

public sealed class ErpCostCentreMappingConfiguration : IEntityTypeConfiguration<ErpCostCentreMapping>
{
    public const int CostCentreLength = 20;

    public void Configure(EntityTypeBuilder<ErpCostCentreMapping> builder)
    {
        // WellId is not a foreign key to Wells: volumes can come from wells outside the loaded master data,
        // and those still need a cost centre before they can be exported.
        builder.Property(mapping => mapping.WellId).HasMaxLength(20).IsUnicode(false);
        builder.Property(mapping => mapping.CostCentre).HasMaxLength(CostCentreLength).IsUnicode(false);
        builder.ConfigureAuditFields();

        builder.HasIndex(mapping => mapping.WellId).IsUnique().HasDatabaseName("UX_ErpCostCentreMappings_WellId");
    }
}

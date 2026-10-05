using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WellData.Core.Entities;

namespace WellData.Infrastructure.Persistence.Configurations;

public sealed class WellFacilityLinkConfiguration : IEntityTypeConfiguration<WellFacilityLink>
{
    public void Configure(EntityTypeBuilder<WellFacilityLink> builder)
    {
        builder
            .HasIndex(link => new { link.WellId, link.FacilityId, link.StartDate })
            .IsUnique()
            .HasDatabaseName("UX_WellFacilityLinks_Well_Facility_StartDate");
        builder.HasIndex(link => link.FacilityId).HasDatabaseName("IX_WellFacilityLinks_FacilityId");

        builder
            .HasOne(link => link.Well)
            .WithMany(well => well.FacilityLinks)
            .HasForeignKey(link => link.WellId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(link => link.Facility)
            .WithMany(facility => facility.WellLinks)
            .HasForeignKey(link => link.FacilityId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

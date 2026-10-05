using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WellData.Core.Entities;

namespace WellData.Infrastructure.Persistence.Configurations;

public sealed class FacilityConfiguration : IEntityTypeConfiguration<Facility>
{
    public void Configure(EntityTypeBuilder<Facility> builder)
    {
        builder.Property(facility => facility.FacilityId).HasMaxLength(20).IsUnicode(false);
        builder.Property(facility => facility.FacilityType).HasMaxLength(2).IsUnicode(false);
        builder.Property(facility => facility.Name).HasMaxLength(100);
        builder.Property(facility => facility.SubType).HasMaxLength(3).IsUnicode(false);
        builder.Property(facility => facility.SubTypeDescription).HasMaxLength(100);
        builder.Property(facility => facility.Location).HasMaxLength(20).IsUnicode(false);
        builder.Property(facility => facility.OperationalStatus).HasMaxLength(30);

        builder.HasIndex(facility => facility.FacilityId).IsUnique().HasDatabaseName("UX_Facilities_FacilityId");
        builder.HasIndex(facility => facility.OperatorId).HasDatabaseName("IX_Facilities_OperatorId");

        builder
            .HasOne(facility => facility.Operator)
            .WithMany(ba => ba.Facilities)
            .HasForeignKey(facility => facility.OperatorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

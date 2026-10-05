using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WellData.Core.Entities;
using WellData.Infrastructure.Persistence.Seed;

namespace WellData.Infrastructure.Persistence.Configurations;

public sealed class ActivityCodeConfiguration : IEntityTypeConfiguration<ActivityCode>
{
    public void Configure(EntityTypeBuilder<ActivityCode> builder)
    {
        builder.HasKey(activity => activity.Code);
        builder.Property(activity => activity.Code).HasMaxLength(10).IsUnicode(false);
        builder.Property(activity => activity.Description).HasMaxLength(100);
        builder.HasData(ReferenceData.ActivityCodes);
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WellData.Core.Entities;
using WellData.Infrastructure.Persistence.Seed;

namespace WellData.Infrastructure.Persistence.Configurations;

public sealed class ProductCodeConfiguration : IEntityTypeConfiguration<ProductCode>
{
    public void Configure(EntityTypeBuilder<ProductCode> builder)
    {
        builder.HasKey(product => product.Code);
        builder.Property(product => product.Code).HasMaxLength(10).IsUnicode(false);
        builder.Property(product => product.Description).HasMaxLength(100);
        builder.HasData(ReferenceData.ProductCodes);
    }
}

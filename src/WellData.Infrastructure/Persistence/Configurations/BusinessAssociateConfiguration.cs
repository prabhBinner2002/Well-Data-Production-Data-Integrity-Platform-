using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WellData.Core.Entities;

namespace WellData.Infrastructure.Persistence.Configurations;

public sealed class BusinessAssociateConfiguration : IEntityTypeConfiguration<BusinessAssociate>
{
    public void Configure(EntityTypeBuilder<BusinessAssociate> builder)
    {
        builder.Property(ba => ba.BaId).HasMaxLength(4).IsUnicode(false);
        builder.Property(ba => ba.LegalName).HasMaxLength(150);
        builder.Property(ba => ba.AbbreviatedName).HasMaxLength(50);
        builder.Property(ba => ba.CorporateStatus).HasMaxLength(30);

        builder.HasIndex(ba => ba.BaId).IsUnique().HasDatabaseName("UX_BusinessAssociates_BaId");
    }
}

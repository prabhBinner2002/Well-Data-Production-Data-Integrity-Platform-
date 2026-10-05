using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WellData.Core.Entities;

namespace WellData.Infrastructure.Persistence.Configurations;

public sealed class SourceFileConfiguration : IEntityTypeConfiguration<SourceFile>
{
    public void Configure(EntityTypeBuilder<SourceFile> builder)
    {
        builder.Property(file => file.Url).HasMaxLength(300);
        builder.Property(file => file.FileName).HasMaxLength(100);
        builder.Property(file => file.FileType).HasConversion<string>().HasMaxLength(20).IsUnicode(false);
        builder.Property(file => file.Status).HasConversion<string>().HasMaxLength(20).IsUnicode(false);
        builder.Property(file => file.Sha256).HasMaxLength(64).IsUnicode(false).IsFixedLength();

        // One row per distinct version of a file. SQL Server treats NULL months as equal in a unique index.
        builder
            .HasIndex(file => new { file.FileType, file.ProductionMonth, file.Sha256 })
            .IsUnique()
            .HasDatabaseName("UX_SourceFiles_Type_Month_Hash");
    }
}

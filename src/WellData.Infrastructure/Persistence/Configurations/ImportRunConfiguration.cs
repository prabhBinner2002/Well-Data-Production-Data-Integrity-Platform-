using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WellData.Core.Entities;

namespace WellData.Infrastructure.Persistence.Configurations;

public sealed class ImportRunConfiguration : IEntityTypeConfiguration<ImportRun>
{
    public void Configure(EntityTypeBuilder<ImportRun> builder)
    {
        builder.Property(run => run.Status).HasConversion<string>().HasMaxLength(20).IsUnicode(false);
        builder.Property(run => run.ErrorMessage).HasMaxLength(2000);

        builder
            .HasOne(run => run.SourceFile)
            .WithMany()
            .HasForeignKey(run => run.SourceFileId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(run => run.ProductionMonth).HasDatabaseName("IX_ImportRuns_ProductionMonth");
    }
}

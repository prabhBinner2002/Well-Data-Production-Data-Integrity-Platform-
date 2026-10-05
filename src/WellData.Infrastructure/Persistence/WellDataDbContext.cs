using Microsoft.EntityFrameworkCore;
using WellData.Core;
using WellData.Core.Entities;

namespace WellData.Infrastructure.Persistence;

public class WellDataDbContext(DbContextOptions<WellDataDbContext> options) : DbContext(options)
{
    public DbSet<ProductCode> ProductCodes => Set<ProductCode>();

    public DbSet<ActivityCode> ActivityCodes => Set<ActivityCode>();

    public DbSet<BusinessAssociate> BusinessAssociates => Set<BusinessAssociate>();

    public DbSet<Facility> Facilities => Set<Facility>();

    public DbSet<Well> Wells => Set<Well>();

    public DbSet<WellFacilityLink> WellFacilityLinks => Set<WellFacilityLink>();

    public DbSet<SourceFile> SourceFiles => Set<SourceFile>();

    public DbSet<StagedVolumetricRow> StagedVolumetricRows => Set<StagedVolumetricRow>();

    public DbSet<VolumetricRecord> VolumetricRecords => Set<VolumetricRecord>();

    public DbSet<ImportRun> ImportRuns => Set<ImportRun>();

    public DbSet<DataQualityRule> DataQualityRules => Set<DataQualityRule>();

    public DbSet<DataQualityIssue> DataQualityIssues => Set<DataQualityIssue>();

    public DbSet<MonthlyWellProduction> MonthlyWellProduction => Set<MonthlyWellProduction>();

    public DbSet<ProductionPeriod> ProductionPeriods => Set<ProductionPeriod>();

    public DbSet<ErpCostCentreMapping> ErpCostCentreMappings => Set<ErpCostCentreMapping>();

    public DbSet<ErpExport> ErpExports => Set<ErpExport>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder
            .Properties<ProductionMonth>()
            .HaveConversion<ProductionMonthConverter>()
            .HaveColumnType("date");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(WellDataDbContext).Assembly);
    }
}

using Microsoft.Extensions.Logging.Abstractions;
using WellData.Core;
using WellData.Core.Validation;
using WellData.Infrastructure.Ingestion;
using WellData.Infrastructure.Ingestion.Master;
using WellData.Infrastructure.Ingestion.Volumetric;
using WellData.Infrastructure.Periods;
using WellData.Infrastructure.Persistence;
using Microsoft.Extensions.Options;
using WellData.Infrastructure.Petrinex;
using WellData.Infrastructure.Scheduling;
using WellData.Infrastructure.Validation;

namespace WellData.Tests.Integration.Infrastructure;

/// <summary>Wires the ingestion services by hand against a test database and fixture files.</summary>
public sealed class IngestionHarness(TestDatabase database)
{
    public static readonly ProductionMonth FixtureMonth = new(2026, 6);

    public static readonly string[] Subset = ["0026", "0XZ4", "A04N"];

    public FixturePetrinexClient Petrinex { get; } = CreateClient();

    public TimeProvider Clock { get; set; } = TimeProvider.System;

    public async Task<IReadOnlyList<MasterFileResult>> IngestMasterAsync()
    {
        await using var context = database.CreateContext();
        var service = new MasterDataIngestionService(
            Petrinex,
            new SourceFileRegistry(context, Clock),
            new MasterDataMerger(context),
            new OperatorSubset(Subset),
            context,
            Clock,
            NullLogger<MasterDataIngestionService>.Instance);

        return await service.IngestAsync(CancellationToken.None);
    }

    public async Task<ImportResult> IngestMonthAsync(ProductionMonth month)
    {
        await using var context = database.CreateContext();
        return await CreateIngestionService(context).IngestMonthAsync(month, CancellationToken.None);
    }

    public Task<ImportResult> IngestFixtureMonthAsync() => IngestMonthAsync(FixtureMonth);

    public VolumetricIngestionService CreateIngestionService(WellDataDbContext context) =>
        new(
            Petrinex,
            new SourceFileRegistry(context, Clock),
            new StagingBulkLoader(context),
            new VolumetricPromoter(context, new VolumetricRecordBulkWriter(context), ValidationEngine.CreateDefault(), Clock),
            CreateSqlRuleRunner(context),
            new PeriodGuard(context),
            new OperatorSubset(Subset),
            context,
            Clock,
            NullLogger<VolumetricIngestionService>.Instance);

    public ValidationOptions Thresholds { get; } = new();

    public IngestionSchedulerOptions Scheduler { get; } = new();

    public async Task<IReadOnlyList<ImportResult>> RunScheduledCheckAsync()
    {
        await using var context = database.CreateContext();
        var checker = new ScheduledMonthChecker(
            CreateIngestionService(context),
            Options.Create(Scheduler),
            Clock,
            NullLogger<ScheduledMonthChecker>.Instance);

        return await checker.CheckAsync(CancellationToken.None);
    }

    public SqlRuleRunner CreateSqlRuleRunner(WellDataDbContext context) =>
        new(context, Options.Create(Thresholds), Clock);

    public async Task<int> RunSqlRulesAsync(ProductionMonth month)
    {
        await using var context = database.CreateContext();
        return await CreateSqlRuleRunner(context).RunAsync(month, CancellationToken.None);
    }

    private static FixturePetrinexClient CreateClient()
    {
        var client = new FixturePetrinexClient();
        client.SetVolumetricFile(FixtureMonth, FixtureFiles.PathTo(FixtureFiles.Volumetric));
        return client;
    }
}

using Microsoft.EntityFrameworkCore;
using WellData.Core.Entities;
using WellData.Core.Validation;
using WellData.Infrastructure.Ingestion.Volumetric;
using WellData.Tests.Integration.Infrastructure;

namespace WellData.Tests.Integration.Ingestion;

public class FailedLoadTests(SqlServerFixture sqlServer) : DatabaseTest(sqlServer)
{
    [Fact]
    public async Task A_load_that_fails_part_way_leaves_no_rows_and_records_the_failure()
    {
        var harness = await FailingHarnessAsync();

        var result = await harness.IngestFixtureMonthAsync();

        Assert.Equal(ImportOutcome.Failed, result.Outcome);
        await using var context = Database.CreateContext();
        Assert.Equal(0, await context.VolumetricRecords.CountAsync());

        var run = await context.ImportRuns.AsNoTracking().SingleAsync();
        Assert.Equal(ImportRunStatus.Failed, run.Status);
        Assert.Equal(130, run.RowsStaged);
        Assert.Equal(0, run.RowsLoaded);
        Assert.False(string.IsNullOrWhiteSpace(run.ErrorMessage));
        Assert.NotNull(run.CompletedAt);

        var file = await context.SourceFiles.AsNoTracking().SingleAsync();
        Assert.Equal(SourceFileStatus.Failed, file.Status);
    }

    [Fact]
    public async Task A_failed_reload_keeps_the_previously_loaded_month_intact()
    {
        await new IngestionHarness(Database).IngestFixtureMonthAsync();

        var result = await (await FailingHarnessAsync()).IngestFixtureMonthAsync();

        Assert.Equal(ImportOutcome.Failed, result.Outcome);
        await using var context = Database.CreateContext();
        Assert.Equal(124, await context.VolumetricRecords.CountAsync());
        Assert.Equal(1, await context.SourceFiles.CountAsync(file => file.Status == SourceFileStatus.Loaded));
    }

    /// <summary>
    /// An activity code that is not in the reference table violates the foreign key during the bulk insert,
    /// after the month's existing rows were deleted and other rows were written in the same transaction.
    /// REF-02 normally quarantines such a row first, so the rule is switched off to let it reach the database.
    /// </summary>
    private async Task<IngestionHarness> FailingHarnessAsync()
    {
        await using (var context = Database.CreateContext())
        {
            await context.DataQualityRules
                .Where(rule => rule.Code == RuleCatalog.UnknownCode)
                .ExecuteUpdateAsync(setters => setters.SetProperty(rule => rule.IsEnabled, false));
        }

        var harness = new IngestionHarness(Database);
        harness.Petrinex.SetVolumetricFile(
            IngestionHarness.FixtureMonth,
            FixtureEditor.CopyWith(FixtureFiles.Volumetric, lines =>
                lines.ReplaceInFirstMatch(",FLARE,GAS,ABBT0131180,", ",BOGUS,GAS,ABBT0131180,")));
        return harness;
    }
}

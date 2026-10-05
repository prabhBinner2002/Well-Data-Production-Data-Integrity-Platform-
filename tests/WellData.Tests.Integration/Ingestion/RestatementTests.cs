using Microsoft.EntityFrameworkCore;
using WellData.Core.Entities;
using WellData.Core.Validation;
using WellData.Infrastructure.Ingestion.Volumetric;
using WellData.Tests.Integration.Infrastructure;

namespace WellData.Tests.Integration.Ingestion;

public class RestatementTests(SqlServerFixture sqlServer) : DatabaseTest(sqlServer)
{
    private const string RestatedWell = "ABWI100013604905W500";

    [Fact]
    public async Task Regenerated_file_replaces_the_month_instead_of_adding_to_it()
    {
        var restated = await LoadOriginalThenRestatedAsync();

        Assert.Equal(ImportOutcome.Loaded, restated.Outcome);
        await using var context = Database.CreateContext();
        Assert.Equal(123, await context.VolumetricRecords.CountAsync());

        var statuses = await context.SourceFiles
            .AsNoTracking()
            .OrderBy(file => file.Id)
            .Select(file => file.Status)
            .ToListAsync();
        Assert.Equal([SourceFileStatus.Superseded, SourceFileStatus.Loaded], statuses);
        Assert.Equal(2, await context.ImportRuns.CountAsync(run => run.Status == ImportRunStatus.Succeeded));
    }

    [Fact]
    public async Task Restated_value_and_its_previous_version_can_both_be_queried()
    {
        await LoadOriginalThenRestatedAsync();

        await using var context = Database.CreateContext();
        var versions = await context.VolumetricRecords
            .TemporalAll()
            .AsNoTracking()
            .Where(record => record.ActivityId == "PROD" && record.ProductId == "OIL" && record.FromToId == RestatedWell)
            .OrderBy(record => EF.Property<DateTime>(record, "PeriodStart"))
            .Select(record => record.Volume)
            .ToListAsync();

        Assert.Equal([38.6m, 41.2m], versions);
    }

    [Fact]
    public async Task Data_can_be_read_as_it_was_before_the_restatement()
    {
        var harness = new IngestionHarness(Database);
        await harness.IngestFixtureMonthAsync();
        var betweenLoads = await ServerUtcNowAsync();
        harness.Petrinex.SetVolumetricFile(IngestionHarness.FixtureMonth, FixtureFiles.PathTo(FixtureFiles.VolumetricRestated));
        await harness.IngestFixtureMonthAsync();

        await using var context = Database.CreateContext();
        var asOf = context.VolumetricRecords.TemporalAsOf(betweenLoads).AsNoTracking();

        Assert.Equal(124, await asOf.CountAsync());
        var volume = await asOf
            .Where(record => record.ActivityId == "PROD" && record.ProductId == "OIL" && record.FromToId == RestatedWell)
            .Select(record => record.Volume)
            .SingleAsync();
        Assert.Equal(38.6m, volume);
    }

    [Fact]
    public async Task RST_01_reports_each_changed_value_with_old_and_new()
    {
        await LoadOriginalThenRestatedAsync();

        var issues = await GetRestatementIssuesAsync();

        Assert.Equal(4, issues.Count);

        var volume = Assert.Single(issues, issue => issue.WellId == RestatedWell);
        Assert.Equal("Volume", volume.Field);
        Assert.Equal("41.200", volume.RawValue);
        Assert.Contains("from 38.600 to 41.200", volume.Message);

        var hours = Assert.Single(issues, issue => issue.WellId == "ABWI100052107409W602");
        Assert.Equal("Hours", hours.Field);
        Assert.Contains("from 433 to 455", hours.Message);
    }

    [Fact]
    public async Task RST_01_reports_a_value_that_was_unmasked_and_a_row_that_was_removed()
    {
        await LoadOriginalThenRestatedAsync();

        var issues = await GetRestatementIssuesAsync();

        var unmasked = Assert.Single(issues, issue => issue.WellId == "ABWI100163606223W400");
        Assert.Equal("Hours", unmasked.Field);
        Assert.Contains("from (none) to 698", unmasked.Message);

        var removed = Assert.Single(issues, issue => issue.Message.Contains("removed"));
        Assert.Equal("ABBT0058572", removed.FacilityId);
        Assert.Contains("FLARE GAS", removed.Message);
    }

    [Fact]
    public async Task First_load_of_a_month_is_not_a_restatement()
    {
        await new IngestionHarness(Database).IngestFixtureMonthAsync();

        Assert.Empty(await GetRestatementIssuesAsync());
    }

    [Fact]
    public async Task Open_file_issues_are_recomputed_for_the_new_version()
    {
        await LoadOriginalThenRestatedAsync();

        // The original has 19 masked values. The restatement unmasks one of them.
        await using var context = Database.CreateContext();
        var masked = await context.DataQualityIssues.CountAsync(issue => issue.RuleCode == RuleCatalog.MaskedValue);
        var sourceFileIds = await context.DataQualityIssues
            .Where(issue => issue.SourceFileId != null)
            .Select(issue => issue.SourceFileId)
            .Distinct()
            .CountAsync();

        Assert.Equal(18, masked);
        Assert.Equal(1, sourceFileIds);
    }

    [Fact]
    public async Task Loading_the_restated_file_again_is_skipped()
    {
        var harness = new IngestionHarness(Database);
        await harness.IngestFixtureMonthAsync();
        harness.Petrinex.SetVolumetricFile(IngestionHarness.FixtureMonth, FixtureFiles.PathTo(FixtureFiles.VolumetricRestated));
        await harness.IngestFixtureMonthAsync();

        var third = await harness.IngestFixtureMonthAsync();

        Assert.Equal(ImportOutcome.Unchanged, third.Outcome);
    }

    [Fact]
    public async Task Reverting_to_the_original_file_clears_the_restatement_issues()
    {
        var harness = new IngestionHarness(Database);
        await harness.IngestFixtureMonthAsync();
        harness.Petrinex.SetVolumetricFile(IngestionHarness.FixtureMonth, FixtureFiles.PathTo(FixtureFiles.VolumetricRestated));
        await harness.IngestFixtureMonthAsync();
        harness.Petrinex.SetVolumetricFile(IngestionHarness.FixtureMonth, FixtureFiles.PathTo(FixtureFiles.Volumetric));

        var reverted = await harness.IngestFixtureMonthAsync();

        Assert.Equal(ImportOutcome.Loaded, reverted.Outcome);
        Assert.Empty(await GetRestatementIssuesAsync());
        await using var context = Database.CreateContext();
        Assert.Equal(124, await context.VolumetricRecords.CountAsync());
    }

    private async Task<ImportResult> LoadOriginalThenRestatedAsync()
    {
        var harness = new IngestionHarness(Database);
        await harness.IngestFixtureMonthAsync();
        harness.Petrinex.SetVolumetricFile(IngestionHarness.FixtureMonth, FixtureFiles.PathTo(FixtureFiles.VolumetricRestated));
        return await harness.IngestFixtureMonthAsync();
    }

    private async Task<List<DataQualityIssue>> GetRestatementIssuesAsync()
    {
        await using var context = Database.CreateContext();
        return await context.DataQualityIssues
            .AsNoTracking()
            .Where(issue => issue.RuleCode == RuleCatalog.Restated)
            .ToListAsync();
    }

    /// <summary>Temporal periods use the database server's clock, which can differ from the test machine's.</summary>
    private async Task<DateTime> ServerUtcNowAsync()
    {
        await using var context = Database.CreateContext();
        return await context.Database.SqlQuery<DateTime>($"SELECT SYSUTCDATETIME() AS Value").SingleAsync();
    }
}

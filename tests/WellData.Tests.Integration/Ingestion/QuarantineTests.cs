using Microsoft.EntityFrameworkCore;
using WellData.Core.Entities;
using WellData.Core.Validation;
using WellData.Tests.Integration.Infrastructure;

namespace WellData.Tests.Integration.Ingestion;

public class QuarantineTests(SqlServerFixture sqlServer) : DatabaseTest(sqlServer)
{
    [Fact]
    public async Task Rows_with_errors_are_not_promoted()
    {
        var result = await IngestErrorFixtureAsync();

        Assert.Equal(131, result.RowsStaged);
        Assert.Equal(118, result.RowsLoaded);
        Assert.Equal(7, result.RowsRejected);
        Assert.Equal(6, result.RowsSkipped);

        await using var context = Database.CreateContext();
        var records = context.VolumetricRecords.AsNoTracking();
        Assert.False(await records.AnyAsync(record => record.Volume < 0 && record.ActivityId == "PROD"));
        Assert.False(await records.AnyAsync(record => record.Hours > 720));
        Assert.False(await records.AnyAsync(record => record.ActivityId == "PROD" && record.ProductId == "OIL" && record.FromToId == "ABWI100013604905W500"));
    }

    [Fact]
    public async Task Rejected_rows_stay_in_staging_with_their_raw_values()
    {
        await IngestErrorFixtureAsync();

        await using var context = Database.CreateContext();
        var raw = await context.StagedVolumetricRows
            .AsNoTracking()
            .SingleAsync(row => row.Volume == "thirty-eight");

        Assert.Equal("ABWI100013604905W500", raw.FromToId);
    }

    [Fact]
    public async Task Row_with_only_a_warning_is_promoted_and_flagged()
    {
        await IngestErrorFixtureAsync();

        await using var context = Database.CreateContext();
        var flagged = await context.VolumetricRecords
            .AsNoTracking()
            .Where(record => record.HasWarning)
            .SingleAsync();

        Assert.Equal("ABWI102023604905W500", flagged.FromToId);
        Assert.Equal(0, flagged.Hours);
        Assert.Equal(49.9m, flagged.Volume);
    }

    [Fact]
    public async Task Legitimate_negative_on_DIFF_is_loaded_without_an_issue()
    {
        await IngestErrorFixtureAsync();

        await using var context = Database.CreateContext();
        Assert.True(await context.VolumetricRecords.AnyAsync(record => record.ActivityId == "DIFF" && record.Volume == -1.5m));
        Assert.False(await context.DataQualityIssues.AnyAsync(issue => issue.RawValue == "-1.5"));
    }

    [Fact]
    public async Task Each_injected_error_is_recorded_as_an_issue()
    {
        await IngestErrorFixtureAsync();

        await using var context = Database.CreateContext();
        var counts = await context.DataQualityIssues
            .AsNoTracking()
            .GroupBy(issue => issue.RuleCode)
            .Select(group => new { RuleCode = group.Key, Count = group.Count() })
            .ToDictionaryAsync(row => row.RuleCode, row => row.Count);

        Assert.Equal(2, counts[RuleCatalog.Format]);
        Assert.Equal(1, counts[RuleCatalog.MissingVolume]);
        Assert.Equal(19, counts[RuleCatalog.MaskedValue]);
        Assert.Equal(1, counts[RuleCatalog.NegativeVolume]);
        Assert.Equal(1, counts[RuleCatalog.HoursExceedMonth]);
        Assert.Equal(1, counts[RuleCatalog.VolumeHoursConsistency]);
        Assert.Equal(2, counts[RuleCatalog.UnknownCode]);
    }

    [Fact]
    public async Task Issue_identifies_the_row_field_and_raw_value()
    {
        await IngestErrorFixtureAsync();

        await using var context = Database.CreateContext();
        var issue = await context.DataQualityIssues
            .AsNoTracking()
            .SingleAsync(candidate => candidate.RuleCode == RuleCatalog.HoursExceedMonth);

        Assert.Equal(IngestionHarness.FixtureMonth, issue.ProductionMonth);
        Assert.Equal("ABBT0131180", issue.FacilityId);
        Assert.Equal("ABWI100033604905W500", issue.WellId);
        Assert.Equal("Hours", issue.Field);
        Assert.Equal("745", issue.RawValue);
        Assert.Equal(IssueStatus.Open, issue.Status);
        Assert.NotNull(issue.RowNumber);
        Assert.NotNull(issue.SourceFileId);
    }

    [Fact]
    public async Task Disabled_rule_raises_no_issues_and_rejects_no_rows()
    {
        await using (var context = Database.CreateContext())
        {
            await context.DataQualityRules
                .Where(rule => rule.Code == RuleCatalog.NegativeVolume)
                .ExecuteUpdateAsync(setters => setters.SetProperty(rule => rule.IsEnabled, false));
        }

        var result = await IngestErrorFixtureAsync();

        Assert.Equal(6, result.RowsRejected);
        await using var verify = Database.CreateContext();
        Assert.False(await verify.DataQualityIssues.AnyAsync(issue => issue.RuleCode == RuleCatalog.NegativeVolume));
    }

    [Fact]
    public async Task Clean_month_raises_only_masked_value_infos_from_the_file_rules()
    {
        await new IngestionHarness(Database).IngestFixtureMonthAsync();

        // File rules record the source row. The SQL rules that run after the load (for example REF-01) do not.
        await using var context = Database.CreateContext();
        var fileIssues = context.DataQualityIssues.Where(issue => issue.RowNumber != null);
        var ruleCodes = await fileIssues
            .Select(issue => issue.RuleCode)
            .Distinct()
            .ToListAsync();

        Assert.Equal([RuleCatalog.MaskedValue], ruleCodes);
        Assert.Equal(19, await fileIssues.CountAsync());
    }

    private async Task<ImportRun> IngestErrorFixtureAsync()
    {
        var harness = new IngestionHarness(Database);
        harness.Petrinex.SetVolumetricFile(IngestionHarness.FixtureMonth, FixtureFiles.PathTo(FixtureFiles.VolumetricWithErrors));
        var result = await harness.IngestFixtureMonthAsync();
        return result.Run!;
    }
}

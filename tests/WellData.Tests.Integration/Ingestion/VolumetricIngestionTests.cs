using Microsoft.EntityFrameworkCore;
using WellData.Core.Entities;
using WellData.Infrastructure.Ingestion.Volumetric;
using WellData.Tests.Integration.Infrastructure;

namespace WellData.Tests.Integration.Ingestion;

public class VolumetricIngestionTests(SqlServerFixture sqlServer) : DatabaseTest(sqlServer)
{
    [Fact]
    public async Task Fixture_month_loads_end_to_end_with_expected_row_counts()
    {
        var harness = new IngestionHarness(Database);

        var result = await harness.IngestFixtureMonthAsync();

        Assert.Equal(ImportOutcome.Loaded, result.Outcome);
        var run = result.Run!;
        Assert.Equal(ImportRunStatus.Succeeded, run.Status);
        Assert.Equal(130, run.RowsStaged);
        Assert.Equal(124, run.RowsLoaded);
        Assert.Equal(6, run.RowsSkipped);
        Assert.Equal(0, run.RowsRejected);
        Assert.NotNull(run.CompletedAt);

        await using var context = Database.CreateContext();
        Assert.Equal(130, await context.StagedVolumetricRows.CountAsync());
        Assert.Equal(124, await context.VolumetricRecords.CountAsync());
    }

    [Fact]
    public async Task Rows_from_operators_outside_the_subset_are_never_staged()
    {
        await new IngestionHarness(Database).IngestFixtureMonthAsync();

        await using var context = Database.CreateContext();
        var operators = await context.StagedVolumetricRows
            .Select(row => row.OperatorBaId)
            .Distinct()
            .OrderBy(baId => baId)
            .ToListAsync();

        Assert.Equal(["0XZ4", "A04N"], operators);
    }

    [Fact]
    public async Task Typed_values_and_masking_are_stored_correctly()
    {
        await new IngestionHarness(Database).IngestFixtureMonthAsync();

        await using var context = Database.CreateContext();
        var records = await context.VolumetricRecords.AsNoTracking().ToListAsync();

        var oil = records.Single(record => record is { ActivityId: "PROD", ProductId: "OIL", FromToId: "ABWI100013604905W500" });
        Assert.Equal(IngestionHarness.FixtureMonth, oil.ProductionMonth);
        Assert.Equal(38.6m, oil.Volume);
        Assert.Equal(720, oil.Hours);
        Assert.Equal("WI", oil.FromToIdType);
        Assert.False(oil.IsMasked);

        var confidential = records.Single(record => record is { ActivityId: "PROD", ProductId: "OIL", FromToId: "ABWI100161207406W600" });
        Assert.True(confidential.IsMasked);
        Assert.Null(confidential.Hours);
        Assert.Equal(1164.8m, confidential.Volume);

        Assert.Equal(19, records.Count(record => record.IsMasked));
        Assert.Equal(7, records.Count(record => record.FromToId == "***"));
    }

    [Fact]
    public async Task Shut_in_rows_are_loaded_without_product_or_volume()
    {
        await new IngestionHarness(Database).IngestFixtureMonthAsync();

        await using var context = Database.CreateContext();
        var shutIns = await context.VolumetricRecords
            .AsNoTracking()
            .Where(record => record.ActivityId == "SHUTIN")
            .ToListAsync();

        Assert.Equal(2, shutIns.Count);
        Assert.All(shutIns, record =>
        {
            Assert.Null(record.ProductId);
            Assert.Null(record.Volume);
        });
    }

    [Fact]
    public async Task Loading_the_same_file_again_is_skipped()
    {
        var harness = new IngestionHarness(Database);
        await harness.IngestFixtureMonthAsync();

        var second = await harness.IngestFixtureMonthAsync();

        Assert.Equal(ImportOutcome.Unchanged, second.Outcome);
        Assert.Null(second.Run);
        await using var context = Database.CreateContext();
        Assert.Equal(124, await context.VolumetricRecords.CountAsync());
        Assert.Equal(1, await context.ImportRuns.CountAsync());
    }

    [Fact]
    public async Task Unchanged_file_can_be_reloaded_on_request_without_duplicating_rows()
    {
        var harness = new IngestionHarness(Database);
        await harness.IngestFixtureMonthAsync();

        ImportResult reloaded;
        await using (var context = Database.CreateContext())
        {
            reloaded = await harness
                .CreateIngestionService(context)
                .IngestMonthAsync(IngestionHarness.FixtureMonth, reloadUnchanged: true, CancellationToken.None);
        }

        Assert.Equal(ImportOutcome.Loaded, reloaded.Outcome);
        await using var verify = Database.CreateContext();
        Assert.Equal(124, await verify.VolumetricRecords.CountAsync());
        Assert.Equal(1, await verify.SourceFiles.CountAsync());
        Assert.Equal(2, await verify.ImportRuns.CountAsync());
        Assert.Equal(0, await verify.DataQualityIssues.CountAsync(issue => issue.RuleCode == "RST-01"));
    }

    [Fact]
    public async Task Source_file_is_marked_loaded_with_month_and_hash()
    {
        await new IngestionHarness(Database).IngestFixtureMonthAsync();

        await using var context = Database.CreateContext();
        var file = await context.SourceFiles.AsNoTracking().SingleAsync();

        Assert.Equal(SourceFileType.Volumetric, file.FileType);
        Assert.Equal(IngestionHarness.FixtureMonth, file.ProductionMonth);
        Assert.Equal(SourceFileStatus.Loaded, file.Status);
        Assert.Equal(130, file.RowCount);
    }

    [Fact]
    public async Task Row_with_an_unreadable_volume_is_rejected_and_the_rest_load()
    {
        var harness = new IngestionHarness(Database);
        harness.Petrinex.SetVolumetricFile(
            IngestionHarness.FixtureMonth,
            FixtureEditor.CopyWith(FixtureFiles.Volumetric, lines =>
                lines.ReplaceInFirstMatch("ABWI100013604905W500,AB,WI,100013604905W500,38.6,", "ABWI100013604905W500,AB,WI,100013604905W500,thirty-eight,")));

        var result = await harness.IngestFixtureMonthAsync();

        Assert.Equal(123, result.Run!.RowsLoaded);
        Assert.Equal(1, result.Run.RowsRejected);
    }
}

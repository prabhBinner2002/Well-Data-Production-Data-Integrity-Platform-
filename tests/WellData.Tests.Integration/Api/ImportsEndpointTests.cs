using System.Net;
using System.Net.Http.Json;
using WellData.Api.Dtos;
using WellData.Core.Entities;
using WellData.Infrastructure.Ingestion.Volumetric;
using WellData.Tests.Integration.Infrastructure;

namespace WellData.Tests.Integration.Api;

public class ImportsEndpointTests(SqlServerFixture sqlServer) : ApiTest(sqlServer)
{
    [Fact]
    public async Task Trigger_imports_the_month_and_returns_the_run()
    {
        var response = await Client.PostAsJsonAsync("/api/imports", new { month = "2026-06" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await ReadAsync<ImportResultDto>(response);
        Assert.Equal(ImportOutcome.Loaded, result.Outcome);
        Assert.Equal(IngestionHarness.FixtureMonth, result.Month);
        Assert.Equal(ImportRunStatus.Succeeded, result.Run!.Status);
        Assert.Equal(124, result.Run.RowsLoaded);
        Assert.Equal(6, result.Run.RowsSkipped);
        Assert.Equal(64, result.Run.SourceFileHash.Length);
    }

    [Fact]
    public async Task Triggering_the_same_month_again_reports_unchanged()
    {
        await Client.PostAsJsonAsync("/api/imports", new { month = "2026-06" });

        var response = await Client.PostAsJsonAsync("/api/imports", new { month = "2026-06" });

        var result = await ReadAsync<ImportResultDto>(response);
        Assert.Equal(ImportOutcome.Unchanged, result.Outcome);
        Assert.Null(result.Run);
    }

    [Fact]
    public async Task Trigger_for_a_month_Petrinex_has_not_published_is_404()
    {
        var response = await Client.PostAsJsonAsync("/api/imports", new { month = "2030-01" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        AssertProblemDetails(response);
    }

    [Theory]
    [InlineData("June 2026")]
    [InlineData("2026-13")]
    [InlineData("")]
    public async Task Trigger_rejects_a_month_that_is_not_YYYY_MM(string month)
    {
        var response = await Client.PostAsJsonAsync("/api/imports", new { month });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        AssertProblemDetails(response);
    }

    [Fact]
    public async Task List_shows_runs_newest_first_with_their_source_file()
    {
        await Harness.IngestFixtureMonthAsync();
        Harness.Petrinex.SetVolumetricFile(IngestionHarness.FixtureMonth, FixtureFiles.PathTo(FixtureFiles.VolumetricRestated));
        await Harness.IngestFixtureMonthAsync();

        var page = await GetAsync<PagedResult<ImportRunDto>>("/api/imports");

        Assert.Equal(2, page.TotalCount);
        Assert.Equal(123, page.Items[0].RowsLoaded);
        Assert.Equal(SourceFileStatus.Loaded, page.Items[0].SourceFileStatus);
        Assert.Equal(SourceFileStatus.Superseded, page.Items[1].SourceFileStatus);
        Assert.True(page.Items[0].StartedAt >= page.Items[1].StartedAt);
    }
}

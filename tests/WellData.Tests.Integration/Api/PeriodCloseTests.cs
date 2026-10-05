using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using WellData.Api.Auth;
using WellData.Api.Dtos;
using WellData.Core.Entities;
using WellData.Infrastructure.Ingestion.Volumetric;
using WellData.Tests.Integration.Infrastructure;

namespace WellData.Tests.Integration.Api;

public class PeriodCloseTests(SqlServerFixture sqlServer) : ApiTest(sqlServer)
{
    [Fact]
    public async Task Loaded_month_is_listed_as_open()
    {
        await SeedFixtureDataAsync();

        var period = Assert.Single(await GetAsync<List<PeriodDto>>("/api/periods"));

        Assert.Equal(IngestionHarness.FixtureMonth, period.ProductionMonth);
        Assert.Equal(PeriodStatus.Open, period.Status);
        Assert.Null(period.ClosedBy);
    }

    [Fact]
    public async Task Steward_can_close_a_month_and_the_close_is_attributed()
    {
        await SeedFixtureDataAsync();

        var response = await Client.PostAsync("/api/periods/2026-06/close", content: null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var closed = await ReadAsync<PeriodDto>(response);
        Assert.Equal(PeriodStatus.Closed, closed.Status);
        Assert.Equal(ApiFactory.StewardUserName, closed.ClosedBy);
        Assert.NotNull(closed.ClosedAt);
        Assert.Equal(PeriodStatus.Closed, Assert.Single(await GetAsync<List<PeriodDto>>("/api/periods")).Status);
    }

    [Fact]
    public async Task Closing_a_month_twice_is_409()
    {
        await SeedFixtureDataAsync();
        await Client.PostAsync("/api/periods/2026-06/close", content: null);

        var response = await Client.PostAsync("/api/periods/2026-06/close", content: null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        AssertProblemDetails(response);
    }

    [Fact]
    public async Task Closing_a_month_with_no_data_is_409()
    {
        var response = await Client.PostAsync("/api/periods/2026-06/close", content: null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Closing_an_invalid_month_is_400()
    {
        var response = await Client.PostAsync("/api/periods/2026-13/close", content: null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Viewer_cannot_close_a_month()
    {
        await SeedFixtureDataAsync();
        var viewer = await CreateClientAsync(Roles.Viewer);

        var response = await viewer.PostAsync("/api/periods/2026-06/close", content: null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(PeriodStatus.Open, Assert.Single(await GetAsync<List<PeriodDto>>("/api/periods")).Status);
    }

    [Fact]
    public async Task Closed_month_rejects_a_reimport_and_keeps_its_data()
    {
        await SeedFixtureDataAsync();
        await Client.PostAsync("/api/periods/2026-06/close", content: null);
        Harness.Petrinex.SetVolumetricFile(IngestionHarness.FixtureMonth, FixtureFiles.PathTo(FixtureFiles.VolumetricRestated));

        var response = await Client.PostAsJsonAsync("/api/imports", new { month = "2026-06" });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        AssertProblemDetails(response);
        await using var context = Database.CreateContext();
        Assert.Equal(124, await context.VolumetricRecords.CountAsync());
        Assert.Equal(1, await context.SourceFiles.CountAsync(file => file.FileType == SourceFileType.Volumetric));
    }

    [Fact]
    public async Task Closed_month_is_skipped_by_the_scheduled_check()
    {
        await SeedFixtureDataAsync();
        await Client.PostAsync("/api/periods/2026-06/close", content: null);
        Harness.Petrinex.SetVolumetricFile(IngestionHarness.FixtureMonth, FixtureFiles.PathTo(FixtureFiles.VolumetricRestated));
        Harness.Clock = new ManualTimeProvider(new DateTimeOffset(2026, 7, 10, 0, 0, 0, TimeSpan.Zero));
        Harness.Scheduler.LookbackMonths = 1;

        var result = Assert.Single(await Harness.RunScheduledCheckAsync());

        Assert.Equal(ImportOutcome.PeriodClosed, result.Outcome);
        await using var context = Database.CreateContext();
        Assert.Equal(124, await context.VolumetricRecords.CountAsync());
    }

    [Fact]
    public async Task Closed_month_rejects_issue_changes()
    {
        await SeedFixtureDataAsync();
        var issue = (await GetAsync<PagedResult<IssueDto>>("/api/issues?ruleCode=REF-01&pageSize=1")).Items[0];
        await Client.PostAsync("/api/periods/2026-06/close", content: null);

        var acknowledge = await Client.PostAsJsonAsync($"/api/issues/{issue.Id}/acknowledge", new { rowVersion = issue.RowVersion });
        var resolve = await Client.PostAsJsonAsync($"/api/issues/{issue.Id}/resolve", new { note = "Too late.", rowVersion = issue.RowVersion });

        Assert.Equal(HttpStatusCode.Conflict, acknowledge.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, resolve.StatusCode);
        Assert.Equal(IssueStatus.Open, (await GetAsync<IssueDto>($"/api/issues/{issue.Id}")).Status);
    }

    [Fact]
    public async Task Open_months_are_unaffected_by_another_months_close()
    {
        await SeedFixtureDataAsync();
        await Client.PostAsync("/api/periods/2026-06/close", content: null);
        Harness.Petrinex.SetVolumetricFile(new(2026, 7), FixtureEditor.CopyWith(FixtureFiles.Volumetric, lines =>
            lines.Select((line, index) => index == 0 ? line : "2026-07" + line[7..])));

        var response = await Client.PostAsJsonAsync("/api/imports", new { month = "2026-07" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(ImportOutcome.Loaded, (await ReadAsync<ImportResultDto>(response)).Outcome);
    }
}

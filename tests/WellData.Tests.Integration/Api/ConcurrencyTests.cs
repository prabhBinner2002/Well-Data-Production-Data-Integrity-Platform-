using System.Net;
using System.Net.Http.Json;
using WellData.Api.Dtos;
using WellData.Core.Entities;
using WellData.Core.Validation;
using WellData.Tests.Integration.Infrastructure;

namespace WellData.Tests.Integration.Api;

public class ConcurrencyTests(SqlServerFixture sqlServer) : ApiTest(sqlServer)
{
    [Fact]
    public async Task Second_steward_working_from_a_stale_issue_gets_409_and_overwrites_nothing()
    {
        await SeedFixtureDataAsync();
        var asRead = (await GetAsync<PagedResult<IssueDto>>("/api/issues?ruleCode=REF-01&pageSize=1")).Items[0];

        var first = await Client.PostAsJsonAsync($"/api/issues/{asRead.Id}/acknowledge", new { rowVersion = asRead.RowVersion });
        var second = await Client.PostAsJsonAsync(
            $"/api/issues/{asRead.Id}/resolve",
            new { note = "Resolved from a stale copy.", rowVersion = asRead.RowVersion });

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        AssertProblemDetails(second);
        var current = await GetAsync<IssueDto>($"/api/issues/{asRead.Id}");
        Assert.Equal(IssueStatus.Acknowledged, current.Status);
        Assert.Null(current.ResolutionNote);
    }

    [Fact]
    public async Task Retrying_with_the_fresh_row_version_succeeds()
    {
        await SeedFixtureDataAsync();
        var asRead = (await GetAsync<PagedResult<IssueDto>>("/api/issues?ruleCode=REF-01&pageSize=1")).Items[0];
        var acknowledged = await ReadAsync<IssueDto>(
            await Client.PostAsJsonAsync($"/api/issues/{asRead.Id}/acknowledge", new { rowVersion = asRead.RowVersion }));

        var response = await Client.PostAsJsonAsync(
            $"/api/issues/{asRead.Id}/resolve",
            new { note = "Resolved after re-reading.", rowVersion = acknowledged.RowVersion });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotEqual(asRead.RowVersion, acknowledged.RowVersion);
    }

    [Fact]
    public async Task Stale_rule_update_gets_409_and_the_first_change_wins()
    {
        var asRead = (await GetAsync<List<RuleDto>>("/api/rules")).Single(rule => rule.Code == RuleCatalog.Spike);

        var first = await Client.PutAsJsonAsync($"/api/rules/{asRead.Code}/enabled", new { isEnabled = false, rowVersion = asRead.RowVersion });
        var second = await Client.PutAsJsonAsync($"/api/rules/{asRead.Code}/enabled", new { isEnabled = true, rowVersion = asRead.RowVersion });

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        Assert.False((await GetAsync<List<RuleDto>>("/api/rules")).Single(rule => rule.Code == RuleCatalog.Spike).IsEnabled);
    }
}

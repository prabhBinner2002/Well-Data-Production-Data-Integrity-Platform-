using System.Net;
using System.Net.Http.Json;
using WellData.Api.Dtos;
using WellData.Core.Entities;
using WellData.Core.Validation;
using WellData.Tests.Integration.Infrastructure;

namespace WellData.Tests.Integration.Api;

public class IssuesEndpointTests(SqlServerFixture sqlServer) : ApiTest(sqlServer)
{
    [Theory]
    [InlineData("", 24)]
    [InlineData("ruleCode=REF-01", 5)]
    [InlineData("ruleCode=MSK-01", 19)]
    [InlineData("severity=Info", 19)]
    [InlineData("severity=Warning", 5)]
    [InlineData("severity=Error", 0)]
    [InlineData("status=Open", 24)]
    [InlineData("status=Resolved", 0)]
    [InlineData("month=2026-06", 24)]
    [InlineData("month=2026-05", 0)]
    [InlineData("wellId=ABWI100161207406W600", 6)]
    [InlineData("ruleCode=MSK-01&severity=Info&status=Open&month=2026-06&wellId=ABWI100163606223W400", 6)]
    public async Task List_filters_by_rule_severity_status_month_and_well(string query, int expectedTotal)
    {
        await SeedFixtureDataAsync();

        var page = await GetAsync<PagedResult<IssueDto>>($"/api/issues?{query}");

        Assert.Equal(expectedTotal, page.TotalCount);
    }

    [Fact]
    public async Task List_pages_and_includes_rule_details()
    {
        await SeedFixtureDataAsync();

        var page = await GetAsync<PagedResult<IssueDto>>("/api/issues?ruleCode=REF-01&pageSize=2&page=3");

        var issue = Assert.Single(page.Items);
        Assert.Equal(5, page.TotalCount);
        Assert.Equal(Severity.Warning, issue.Severity);
        Assert.Equal("From/To well is not in master data", issue.RuleDescription);
        Assert.Equal(IssueStatus.Open, issue.Status);
    }

    [Theory]
    [InlineData("severity=Catastrophic")]
    [InlineData("status=Closed")]
    [InlineData("month=2026-6")]
    public async Task List_rejects_unknown_enum_values_and_bad_months(string query)
    {
        var response = await Client.GetAsync($"/api/issues?{query}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        AssertProblemDetails(response);
    }

    [Fact]
    public async Task Get_returns_the_issue_and_404_for_an_unknown_id()
    {
        var seeded = await SeedAndGetIssueAsync(RuleCatalog.UnknownWell);

        var issue = await GetAsync<IssueDto>($"/api/issues/{seeded.Id}");
        var missing = await Client.GetAsync("/api/issues/999999");

        Assert.Equal(RuleCatalog.UnknownWell, issue.RuleCode);
        Assert.Equal("FromToID", issue.Field);
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
    }

    [Fact]
    public async Task Acknowledge_moves_an_open_issue_to_acknowledged()
    {
        var issue = await SeedAndGetIssueAsync(RuleCatalog.UnknownWell);

        var response = await Client.PostAsJsonAsync($"/api/issues/{issue.Id}/acknowledge", new { rowVersion = issue.RowVersion });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(IssueStatus.Acknowledged, (await ReadAsync<IssueDto>(response)).Status);
        Assert.Equal(IssueStatus.Acknowledged, (await GetAsync<IssueDto>($"/api/issues/{issue.Id}")).Status);
    }

    [Fact]
    public async Task Resolve_stores_the_note_and_closes_the_issue()
    {
        var issue = await SeedAndGetIssueAsync(RuleCatalog.UnknownWell);

        var response = await Client.PostAsJsonAsync(
            $"/api/issues/{issue.Id}/resolve",
            new { note = "  Battery sold to another operator in 2026.  ", rowVersion = issue.RowVersion });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var resolved = await ReadAsync<IssueDto>(response);
        Assert.Equal(IssueStatus.Resolved, resolved.Status);
        Assert.Equal("Battery sold to another operator in 2026.", resolved.ResolutionNote);
    }

    [Theory]
    [InlineData("")]
    [InlineData("ok")]
    public async Task Resolve_requires_a_meaningful_note(string note)
    {
        var issue = await SeedAndGetIssueAsync(RuleCatalog.UnknownWell);

        var response = await Client.PostAsJsonAsync($"/api/issues/{issue.Id}/resolve", new { note, rowVersion = issue.RowVersion });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        AssertProblemDetails(response);
        Assert.Equal(IssueStatus.Open, (await GetAsync<IssueDto>($"/api/issues/{issue.Id}")).Status);
    }

    [Fact]
    public async Task Resolved_issue_cannot_be_acknowledged_or_resolved_again()
    {
        var issue = await SeedAndGetIssueAsync(RuleCatalog.UnknownWell);
        var first = await Client.PostAsJsonAsync(
            $"/api/issues/{issue.Id}/resolve",
            new { note = "Checked with the operator.", rowVersion = issue.RowVersion });
        var resolved = await ReadAsync<IssueDto>(first);

        var acknowledge = await Client.PostAsJsonAsync($"/api/issues/{issue.Id}/acknowledge", new { rowVersion = resolved.RowVersion });
        var resolve = await Client.PostAsJsonAsync(
            $"/api/issues/{issue.Id}/resolve",
            new { note = "Second attempt.", rowVersion = resolved.RowVersion });

        Assert.Equal(HttpStatusCode.Conflict, acknowledge.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, resolve.StatusCode);
        AssertProblemDetails(acknowledge);
        Assert.Equal("Checked with the operator.", (await GetAsync<IssueDto>($"/api/issues/{issue.Id}")).ResolutionNote);
    }

    [Fact]
    public async Task Acknowledging_or_resolving_an_unknown_issue_is_404()
    {
        var rowVersion = new byte[8];

        var acknowledge = await Client.PostAsJsonAsync("/api/issues/999999/acknowledge", new { rowVersion });
        var resolve = await Client.PostAsJsonAsync("/api/issues/999999/resolve", new { note = "No such issue.", rowVersion });

        Assert.Equal(HttpStatusCode.NotFound, acknowledge.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, resolve.StatusCode);
    }

    private async Task<IssueDto> SeedAndGetIssueAsync(string ruleCode)
    {
        await SeedFixtureDataAsync();
        var page = await GetAsync<PagedResult<IssueDto>>($"/api/issues?ruleCode={ruleCode}&pageSize=1");
        return page.Items[0];
    }
}

using System.Net;
using System.Net.Http.Json;
using WellData.Api.Dtos;
using WellData.Core.Entities;
using WellData.Core.Validation;
using WellData.Tests.Integration.Infrastructure;

namespace WellData.Tests.Integration.Api;

public class RulesEndpointTests(SqlServerFixture sqlServer) : ApiTest(sqlServer)
{
    [Fact]
    public async Task List_returns_the_whole_catalog_in_code_order()
    {
        var rules = await GetAsync<List<RuleDto>>("/api/rules");

        Assert.Equal(RuleCatalog.FileRules.Length + RuleCatalog.SqlRules.Length, rules.Count);
        Assert.Equal(rules.Select(rule => rule.Code).Order(), rules.Select(rule => rule.Code));
        Assert.All(rules, rule => Assert.True(rule.IsEnabled));
        Assert.Equal(Severity.Error, rules.Single(rule => rule.Code == RuleCatalog.NegativeVolume).Severity);
    }

    [Fact]
    public async Task Disabling_a_rule_persists_and_can_be_reversed()
    {
        var rule = await GetRuleAsync(RuleCatalog.Spike);

        var disabled = await SetEnabledAsync(rule, isEnabled: false);

        Assert.Equal(HttpStatusCode.OK, disabled.StatusCode);
        var afterDisable = await ReadAsync<RuleDto>(disabled);
        Assert.False(afterDisable.IsEnabled);
        Assert.False((await GetRuleAsync(RuleCatalog.Spike)).IsEnabled);

        var enabled = await SetEnabledAsync(afterDisable, isEnabled: true);

        Assert.True((await ReadAsync<RuleDto>(enabled)).IsEnabled);
    }

    [Fact]
    public async Task Disabled_rule_is_skipped_by_the_next_import()
    {
        await SetEnabledAsync(await GetRuleAsync(RuleCatalog.MaskedValue), isEnabled: false);

        await Harness.IngestFixtureMonthAsync();

        var page = await GetAsync<PagedResult<IssueDto>>($"/api/issues?ruleCode={RuleCatalog.MaskedValue}");
        Assert.Equal(0, page.TotalCount);
    }

    [Fact]
    public async Task Unknown_rule_code_is_404()
    {
        var response = await Client.PutAsJsonAsync("/api/rules/XXX-99/enabled", new { isEnabled = false, rowVersion = new byte[8] });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        AssertProblemDetails(response);
    }

    [Fact]
    public async Task Malformed_body_is_400()
    {
        var response = await Client.PutAsJsonAsync($"/api/rules/{RuleCatalog.Spike}/enabled", new { isEnabled = "maybe" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        AssertProblemDetails(response);
    }

    [Fact]
    public async Task Missing_row_version_is_400()
    {
        var response = await Client.PutAsJsonAsync($"/api/rules/{RuleCatalog.Spike}/enabled", new { isEnabled = false });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private async Task<RuleDto> GetRuleAsync(string code) =>
        (await GetAsync<List<RuleDto>>("/api/rules")).Single(rule => rule.Code == code);

    private Task<HttpResponseMessage> SetEnabledAsync(RuleDto rule, bool isEnabled) =>
        Client.PutAsJsonAsync($"/api/rules/{rule.Code}/enabled", new { isEnabled, rowVersion = rule.RowVersion });
}

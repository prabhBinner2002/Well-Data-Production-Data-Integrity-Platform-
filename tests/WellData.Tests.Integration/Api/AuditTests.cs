using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using WellData.Api.Dtos;
using WellData.Core.Validation;
using WellData.Infrastructure.Auditing;
using WellData.Tests.Integration.Infrastructure;

namespace WellData.Tests.Integration.Api;

public class AuditTests(SqlServerFixture sqlServer) : ApiTest(sqlServer)
{
    [Fact]
    public async Task Resolving_an_issue_records_who_did_it_and_when()
    {
        await SeedFixtureDataAsync();
        var issue = (await GetAsync<PagedResult<IssueDto>>("/api/issues?ruleCode=REF-01&pageSize=1")).Items[0];
        var before = DateTime.UtcNow.AddSeconds(-5);

        var response = await Client.PostAsJsonAsync(
            $"/api/issues/{issue.Id}/resolve",
            new { note = "Battery changed operator.", rowVersion = issue.RowVersion });

        var resolved = await ReadAsync<IssueDto>(response);
        Assert.Equal(ApiFactory.StewardUserName, resolved.ModifiedBy);
        Assert.InRange(resolved.ModifiedAt!.Value, before, DateTime.UtcNow.AddSeconds(5));
    }

    [Fact]
    public async Task Changing_a_rule_records_who_did_it()
    {
        var rule = (await GetAsync<List<RuleDto>>("/api/rules")).Single(candidate => candidate.Code == RuleCatalog.Spike);
        Assert.Null(rule.ModifiedBy);

        var response = await Client.PutAsJsonAsync($"/api/rules/{rule.Code}/enabled", new { isEnabled = false, rowVersion = rule.RowVersion });

        Assert.Equal(ApiFactory.StewardUserName, (await ReadAsync<RuleDto>(response)).ModifiedBy);
    }

    [Fact]
    public async Task Issues_raised_by_an_import_record_the_user_who_triggered_it()
    {
        await Client.PostAsJsonAsync("/api/imports", new { month = "2026-06" });

        await using var context = Database.CreateContext();
        var creators = await context.DataQualityIssues
            .Select(issue => issue.CreatedBy)
            .Distinct()
            .ToListAsync();

        Assert.Equal([ApiFactory.StewardUserName], creators);
        Assert.DoesNotContain(await context.DataQualityIssues.Select(issue => issue.CreatedAt).ToListAsync(), createdAt => createdAt is null);
    }

    [Fact]
    public void Work_done_outside_a_request_is_attributed_to_the_system()
    {
        Assert.Equal("system", new SystemUser().Name);
    }
}

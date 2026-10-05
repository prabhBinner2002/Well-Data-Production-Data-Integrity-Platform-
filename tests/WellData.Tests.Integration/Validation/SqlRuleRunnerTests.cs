using Microsoft.EntityFrameworkCore;
using WellData.Core.Entities;
using WellData.Core.Validation;
using WellData.Tests.Integration.Infrastructure;

namespace WellData.Tests.Integration.Validation;

public class SqlRuleRunnerTests(SqlServerFixture sqlServer) : SqlRuleTest(sqlServer)
{
    [Fact]
    public async Task Running_the_rules_twice_does_not_duplicate_issues()
    {
        await SeedDuplicateAsync();

        var raisedFirstTime = await Harness.RunSqlRulesAsync(June);
        var raisedSecondTime = await Harness.RunSqlRulesAsync(June);

        Assert.Equal(raisedFirstTime, raisedSecondTime);
        Assert.Single(await GetIssuesAsync(RuleCatalog.Duplicate));
        await using var context = Database.CreateContext();
        Assert.Equal(raisedFirstTime, await context.DataQualityIssues.CountAsync());
    }

    [Fact]
    public async Task Acknowledged_issue_is_kept_and_not_raised_again()
    {
        await SeedDuplicateAsync();
        await Harness.RunSqlRulesAsync(June);
        await using (var context = Database.CreateContext())
        {
            await context.DataQualityIssues.ExecuteUpdateAsync(setters => setters.SetProperty(issue => issue.Status, IssueStatus.Acknowledged));
        }

        var raised = await Harness.RunSqlRulesAsync(June);

        Assert.Equal(0, raised);
        var issue = Assert.Single(await GetIssuesAsync(RuleCatalog.Duplicate));
        Assert.Equal(IssueStatus.Acknowledged, issue.Status);
    }

    [Fact]
    public async Task Open_issue_disappears_once_the_data_is_fixed()
    {
        await SeedDuplicateAsync();
        await Harness.RunSqlRulesAsync(June);
        await using (var context = Database.CreateContext())
        {
            var extraCopy = await context.VolumetricRecords.OrderBy(record => record.Id).LastAsync();
            context.VolumetricRecords.Remove(extraCopy);
            await context.SaveChangesAsync();
        }

        await Harness.RunSqlRulesAsync(June);

        Assert.Empty(await GetIssuesAsync(RuleCatalog.Duplicate));
    }

    [Fact]
    public async Task Disabled_rule_does_not_run()
    {
        await SeedDuplicateAsync();
        await using (var context = Database.CreateContext())
        {
            await context.DataQualityRules
                .Where(rule => rule.Code == RuleCatalog.Duplicate)
                .ExecuteUpdateAsync(setters => setters.SetProperty(rule => rule.IsEnabled, false));
        }

        Assert.Empty(await RunAndGetIssuesAsync(RuleCatalog.Duplicate));
    }

    [Fact]
    public async Task Rules_only_look_at_the_requested_month()
    {
        await SeedDuplicateAsync();

        Assert.Empty(await RunAndGetIssuesAsync(RuleCatalog.Duplicate, June.AddMonths(1)));
    }

    private Task SeedDuplicateAsync() =>
        Seed
            .Record(June, "PROD", "OIL", 38.6m, VolumetricSeeder.WellA)
            .Record(June, "PROD", "OIL", 38.6m, VolumetricSeeder.WellA)
            .SaveAsync();
}

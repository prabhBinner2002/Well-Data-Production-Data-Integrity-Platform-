using WellData.Core;
using WellData.Core.Validation;
using WellData.Tests.Integration.Infrastructure;

namespace WellData.Tests.Integration.Validation;

public class ReportingGapRuleTests(SqlServerFixture sqlServer) : SqlRuleTest(sqlServer)
{
    private static readonly ProductionMonth March = new(2026, 3);

    [Fact]
    public async Task Well_that_reappears_after_a_two_month_gap_is_reported()
    {
        await Seed
            .Production(March, VolumetricSeeder.WellA, oil: 40)
            .Production(June, VolumetricSeeder.WellA, oil: 38.6m)
            .SaveAsync();

        var issue = Assert.Single(await RunAndGetIssuesAsync(RuleCatalog.ReportingGap));

        Assert.Equal(VolumetricSeeder.WellA, issue.WellId);
        Assert.Contains("2 month(s)", issue.Message);
        Assert.Contains("2026-03", issue.Message);
    }

    [Fact]
    public async Task Consecutive_months_have_no_gap()
    {
        await Seed
            .Production(June.AddMonths(-2), VolumetricSeeder.WellA, oil: 40)
            .Production(June.AddMonths(-1), VolumetricSeeder.WellA, oil: 39)
            .Production(June, VolumetricSeeder.WellA, oil: 38.6m)
            .SaveAsync();

        Assert.Empty(await RunAndGetIssuesAsync(RuleCatalog.ReportingGap));
    }

    [Fact]
    public async Task Month_reported_as_shut_in_is_not_a_gap()
    {
        await Seed
            .Production(June.AddMonths(-2), VolumetricSeeder.WellA, oil: 40)
            .Record(June.AddMonths(-1), "SHUTIN", null, null, VolumetricSeeder.WellA)
            .Production(June, VolumetricSeeder.WellA, oil: 38.6m)
            .SaveAsync();

        Assert.Empty(await RunAndGetIssuesAsync(RuleCatalog.ReportingGap));
    }

    [Fact]
    public async Task First_month_a_well_ever_reports_is_not_a_gap()
    {
        await Seed.Production(June, VolumetricSeeder.WellA, oil: 38.6m).SaveAsync();

        Assert.Empty(await RunAndGetIssuesAsync(RuleCatalog.ReportingGap));
    }

    [Fact]
    public async Task Gap_is_reported_on_the_month_the_well_returns_not_on_later_months()
    {
        await Seed
            .Production(March, VolumetricSeeder.WellA, oil: 40)
            .Production(June.AddMonths(-1), VolumetricSeeder.WellA, oil: 39)
            .Production(June, VolumetricSeeder.WellA, oil: 38.6m)
            .SaveAsync();

        Assert.Empty(await RunAndGetIssuesAsync(RuleCatalog.ReportingGap, June));
        Assert.Single(await RunAndGetIssuesAsync(RuleCatalog.ReportingGap, June.AddMonths(-1)));
    }
}

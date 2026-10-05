using WellData.Core.Validation;
using WellData.Tests.Integration.Infrastructure;

namespace WellData.Tests.Integration.Validation;

public class OutlierRuleTests(SqlServerFixture sqlServer) : SqlRuleTest(sqlServer)
{
    private static readonly decimal[] SteadyHistory = [100, 102, 98, 101, 99, 103, 97, 100];

    [Fact]
    public async Task Volume_far_above_the_trailing_average_is_reported()
    {
        await SeedHistoryThenAsync(current: 180);

        var issue = Assert.Single(await RunAndGetIssuesAsync(RuleCatalog.Outlier));

        Assert.Equal(VolumetricSeeder.WellA, issue.WellId);
        Assert.Equal("OIL", issue.Field);
        Assert.Contains("trailing average of 100.0 over 8 months", issue.Message);
    }

    [Fact]
    public async Task Volume_far_below_the_trailing_average_is_reported()
    {
        await SeedHistoryThenAsync(current: 20);

        Assert.Single(await RunAndGetIssuesAsync(RuleCatalog.Outlier));
    }

    [Fact]
    public async Task Volume_within_normal_variation_is_not_reported()
    {
        await SeedHistoryThenAsync(current: 104);

        Assert.Empty(await RunAndGetIssuesAsync(RuleCatalog.Outlier));
    }

    [Fact]
    public async Task Well_with_too_little_history_is_not_judged()
    {
        await Seed
            .Production(June.AddMonths(-2), VolumetricSeeder.WellA, oil: 100)
            .Production(June.AddMonths(-1), VolumetricSeeder.WellA, oil: 101)
            .Production(June, VolumetricSeeder.WellA, oil: 500)
            .SaveAsync();

        Assert.Empty(await RunAndGetIssuesAsync(RuleCatalog.Outlier));
    }

    [Fact]
    public async Task Each_well_is_judged_against_its_own_history()
    {
        await SeedHistoryThenAsync(current: 101);
        for (var monthsBack = SteadyHistory.Length; monthsBack >= 1; monthsBack--)
        {
            Seed.Production(June.AddMonths(-monthsBack), VolumetricSeeder.WellB, oil: 1000 + monthsBack);
        }

        await Seed.Production(June, VolumetricSeeder.WellB, oil: 1003).SaveAsync();

        Assert.Empty(await RunAndGetIssuesAsync(RuleCatalog.Outlier));
    }

    private async Task SeedHistoryThenAsync(decimal current)
    {
        for (var index = 0; index < SteadyHistory.Length; index++)
        {
            Seed.Production(June.AddMonths(index - SteadyHistory.Length), VolumetricSeeder.WellA, oil: SteadyHistory[index]);
        }

        await Seed.Production(June, VolumetricSeeder.WellA, oil: current).SaveAsync();
    }
}

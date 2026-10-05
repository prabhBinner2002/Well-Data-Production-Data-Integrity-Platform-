using WellData.Core;
using WellData.Core.Validation;
using WellData.Tests.Integration.Infrastructure;

namespace WellData.Tests.Integration.Validation;

public class SpikeRuleTests(SqlServerFixture sqlServer) : SqlRuleTest(sqlServer)
{
    private static readonly ProductionMonth May = new(2026, 5);

    [Fact]
    public async Task Four_hundred_percent_jump_is_reported()
    {
        await Seed
            .Production(May, VolumetricSeeder.WellA, oil: 10)
            .Production(June, VolumetricSeeder.WellA, oil: 50)
            .SaveAsync();

        var issue = Assert.Single(await RunAndGetIssuesAsync(RuleCatalog.Spike));

        Assert.Equal(VolumetricSeeder.WellA, issue.WellId);
        Assert.Equal("OIL", issue.Field);
        Assert.Contains("400.0%", issue.Message);
    }

    [Fact]
    public async Task Change_below_the_threshold_is_not_reported()
    {
        await Seed
            .Production(May, VolumetricSeeder.WellA, oil: 10)
            .Production(June, VolumetricSeeder.WellA, oil: 25)
            .SaveAsync();

        Assert.Empty(await RunAndGetIssuesAsync(RuleCatalog.Spike));
    }

    [Fact]
    public async Task Threshold_is_configurable()
    {
        Harness.Thresholds.SpikeThresholdPercent = 100;
        await Seed
            .Production(May, VolumetricSeeder.WellA, oil: 10)
            .Production(June, VolumetricSeeder.WellA, oil: 25)
            .SaveAsync();

        Assert.Single(await RunAndGetIssuesAsync(RuleCatalog.Spike));
    }

    [Fact]
    public async Task Each_product_is_compared_with_its_own_previous_month()
    {
        await Seed
            .Production(May, VolumetricSeeder.WellA, oil: 10, gas: 100)
            .Production(June, VolumetricSeeder.WellA, oil: 11, gas: 900)
            .SaveAsync();

        var issue = Assert.Single(await RunAndGetIssuesAsync(RuleCatalog.Spike));

        Assert.Equal("GAS", issue.Field);
    }

    [Fact]
    public async Task Tiny_previous_volume_is_ignored()
    {
        await Seed
            .Production(May, VolumetricSeeder.WellA, oil: 0.1m)
            .Production(June, VolumetricSeeder.WellA, oil: 4)
            .SaveAsync();

        Assert.Empty(await RunAndGetIssuesAsync(RuleCatalog.Spike));
    }

    [Fact]
    public async Task Months_that_are_not_consecutive_are_not_compared()
    {
        await Seed
            .Production(June.AddMonths(-3), VolumetricSeeder.WellA, oil: 10)
            .Production(June, VolumetricSeeder.WellA, oil: 50)
            .SaveAsync();

        Assert.Empty(await RunAndGetIssuesAsync(RuleCatalog.Spike));
    }
}

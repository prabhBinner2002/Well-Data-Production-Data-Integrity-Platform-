using WellData.Core.Validation;
using WellData.Tests.Integration.Infrastructure;

namespace WellData.Tests.Integration.Validation;

public class RatioRuleTests(SqlServerFixture sqlServer) : SqlRuleTest(sqlServer)
{
    [Fact]
    public async Task Water_cut_above_the_limit_is_reported()
    {
        await Seed.Production(June, VolumetricSeeder.WellA, oil: 10, gas: 1, water: 1990).SaveAsync();

        var issue = Assert.Single(await RunAndGetIssuesAsync(RuleCatalog.Ratio));

        Assert.Equal("WaterCut", issue.Field);
        Assert.Equal("0.9950", issue.RawValue);
        Assert.Equal(VolumetricSeeder.WellA, issue.WellId);
    }

    [Fact]
    public async Task Gas_oil_ratio_above_the_limit_is_reported_in_m3_per_m3()
    {
        // 500 e3m3 of gas over 10 m3 of oil is 50,000 m3/m3.
        await Seed.Production(June, VolumetricSeeder.WellA, oil: 10, gas: 500, water: 5).SaveAsync();

        var issue = Assert.Single(await RunAndGetIssuesAsync(RuleCatalog.Ratio));

        Assert.Equal("GasOilRatio", issue.Field);
        Assert.Equal("50000.0", issue.RawValue);
    }

    [Fact]
    public async Task Well_can_breach_both_bounds_at_once()
    {
        await Seed.Production(June, VolumetricSeeder.WellA, oil: 10, gas: 500, water: 1990).SaveAsync();

        var fields = (await RunAndGetIssuesAsync(RuleCatalog.Ratio)).Select(issue => issue.Field).Order();

        Assert.Equal(["GasOilRatio", "WaterCut"], fields);
    }

    [Fact]
    public async Task Ordinary_oil_well_is_within_bounds()
    {
        await Seed.Production(June, VolumetricSeeder.WellA, oil: 38.6m, gas: 9.8m, water: 1.0m).SaveAsync();

        Assert.Empty(await RunAndGetIssuesAsync(RuleCatalog.Ratio));
    }

    [Fact]
    public async Task Gas_well_with_almost_no_oil_is_not_judged()
    {
        await Seed.Production(June, VolumetricSeeder.WellA, oil: 0.2m, gas: 2862.6m, water: 400).SaveAsync();
        await Seed.Production(June, VolumetricSeeder.WellB, gas: 1132.3m, water: 1364.7m).SaveAsync();

        Assert.Empty(await RunAndGetIssuesAsync(RuleCatalog.Ratio));
    }

    [Fact]
    public async Task Bounds_are_configurable()
    {
        Harness.Thresholds.MaxWaterCut = 0.5m;
        await Seed.Production(June, VolumetricSeeder.WellA, oil: 10, water: 15).SaveAsync();

        Assert.Single(await RunAndGetIssuesAsync(RuleCatalog.Ratio));
    }
}

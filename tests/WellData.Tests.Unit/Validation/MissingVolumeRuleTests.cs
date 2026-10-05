using WellData.Core.Validation.Rules;

namespace WellData.Tests.Unit.Validation;

public class MissingVolumeRuleTests
{
    private readonly MissingVolumeRule _rule = new();

    [Fact]
    public void Row_with_a_volume_passes()
    {
        Assert.Empty(_rule.Evaluate([TestRows.Row(volume: "0.0")], TestRows.Context()));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Blank_volume_on_PROD_is_an_error(string? volume)
    {
        var violation = Assert.Single(_rule.Evaluate([TestRows.Row(volume: volume)], TestRows.Context()));

        Assert.Equal("Volume", violation.Field);
        Assert.Contains("PROD", violation.Message);
    }

    [Fact]
    public void Blank_volume_on_SHUTIN_is_expected()
    {
        Assert.Empty(_rule.Evaluate([TestRows.Row(activity: "SHUTIN", product: "", volume: null)], TestRows.Context()));
    }

    [Fact]
    public void Masked_volume_is_not_missing()
    {
        Assert.Empty(_rule.Evaluate([TestRows.Row(volume: "***")], TestRows.Context()));
    }

    [Fact]
    public void Blank_volume_on_an_unknown_activity_is_still_an_error()
    {
        Assert.Single(_rule.Evaluate([TestRows.Row(activity: "VNTX", volume: null)], TestRows.Context()));
    }

    [Fact]
    public async Task Real_fixture_month_has_no_missing_volumes()
    {
        Assert.Empty(_rule.Evaluate(await TestRows.FixtureDataRowsAsync(), TestRows.Context()));
    }
}

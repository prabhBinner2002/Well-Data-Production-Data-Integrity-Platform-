using WellData.Core.Validation.Rules;

namespace WellData.Tests.Unit.Validation;

public class NegativeVolumeRuleTests
{
    private readonly NegativeVolumeRule _rule = new();

    [Fact]
    public void Negative_volume_on_PROD_is_an_error()
    {
        var violation = Assert.Single(_rule.Evaluate([TestRows.Row(activity: "PROD", volume: "-0.7")], TestRows.Context()));

        Assert.Equal("Volume", violation.Field);
        Assert.Equal("-0.7", violation.RawValue);
    }

    [Theory]
    [InlineData("DIFF")]
    [InlineData("INVADJ")]
    [InlineData("FRAC")]
    [InlineData("LDINVADJ")]
    [InlineData("PROC")]
    [InlineData("SHR")]
    public void Negative_volume_on_an_adjustment_activity_is_allowed(string activity)
    {
        Assert.Empty(_rule.Evaluate([TestRows.Row(activity: activity, volume: "-1.5", fromTo: null, fromToType: null)], TestRows.Context()));
    }

    [Theory]
    [InlineData("0")]
    [InlineData("0.0")]
    [InlineData("38.6")]
    public void Zero_and_positive_volumes_pass(string volume)
    {
        Assert.Empty(_rule.Evaluate([TestRows.Row(volume: volume)], TestRows.Context()));
    }

    [Fact]
    public void Negative_volume_on_an_unknown_activity_is_left_to_REF_02()
    {
        Assert.Empty(_rule.Evaluate([TestRows.Row(activity: "VNTX", volume: "-5")], TestRows.Context()));
    }

    [Fact]
    public async Task Real_fixture_month_has_no_disallowed_negatives()
    {
        Assert.Empty(_rule.Evaluate(await TestRows.FixtureDataRowsAsync(), TestRows.Context()));
    }
}

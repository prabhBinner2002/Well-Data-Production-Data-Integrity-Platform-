using WellData.Core.Validation.Rules;

namespace WellData.Tests.Unit.Validation;

public class MaskedValueRuleTests
{
    private readonly MaskedValueRule _rule = new();

    [Fact]
    public void Unmasked_row_raises_nothing()
    {
        Assert.Empty(_rule.Evaluate([TestRows.Row(hours: "720")], TestRows.Context()));
    }

    [Fact]
    public void Blank_hours_are_not_masked()
    {
        Assert.Empty(_rule.Evaluate([TestRows.Row(hours: null)], TestRows.Context()));
    }

    [Fact]
    public void Masked_hours_are_reported_against_the_hours_field()
    {
        var violation = Assert.Single(_rule.Evaluate([TestRows.Row(hours: "***")], TestRows.Context()));

        Assert.Equal("Hours", violation.Field);
        Assert.Equal("***", violation.RawValue);
    }

    [Fact]
    public void Each_masked_field_on_a_row_is_reported()
    {
        var row = TestRows.Row(volume: "***", hours: "***", fromTo: "***", fromToType: null);

        var fields = _rule.Evaluate([row], TestRows.Context()).Select(violation => violation.Field).Order();

        Assert.Equal(["FromToID", "Hours", "Volume"], fields);
    }

    [Fact]
    public async Task Real_fixture_month_has_nineteen_masked_values()
    {
        var violations = _rule.Evaluate(await TestRows.FixtureDataRowsAsync(), TestRows.Context()).ToList();

        Assert.Equal(12, violations.Count(violation => violation.Field == "Hours"));
        Assert.Equal(7, violations.Count(violation => violation.Field == "FromToID"));
    }
}

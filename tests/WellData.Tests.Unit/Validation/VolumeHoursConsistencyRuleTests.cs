using WellData.Core.Validation.Rules;

namespace WellData.Tests.Unit.Validation;

public class VolumeHoursConsistencyRuleTests
{
    private readonly VolumeHoursConsistencyRule _rule = new();

    [Fact]
    public void Volume_with_hours_passes()
    {
        Assert.Empty(_rule.Evaluate([TestRows.Row(volume: "38.6", hours: "720")], TestRows.Context()));
    }

    [Fact]
    public void Volume_with_zero_hours_is_a_warning()
    {
        var violation = Assert.Single(_rule.Evaluate([TestRows.Row(volume: "49.9", hours: "0")], TestRows.Context()));

        Assert.Equal("Hours", violation.Field);
    }

    [Fact]
    public void Hours_with_no_volume_is_a_warning()
    {
        var violation = Assert.Single(_rule.Evaluate([TestRows.Row(volume: "0.0", hours: "312")], TestRows.Context()));

        Assert.Equal("Volume", violation.Field);
    }

    [Fact]
    public void Hours_on_one_product_row_cover_the_wells_other_product_rows()
    {
        // The real file reports hours only on the oil row. The gas and water rows are not "volume with no hours".
        var rows = new[]
        {
            TestRows.Row(product: "OIL", volume: "38.6", hours: "720"),
            TestRows.Row(product: "GAS", volume: "9.8", hours: null),
            TestRows.Row(product: "WATER", volume: "1.0", hours: null),
        };

        Assert.Empty(_rule.Evaluate(rows, TestRows.Context()));
    }

    [Fact]
    public void Zero_hours_is_judged_against_the_wells_total_volume()
    {
        var rows = new[]
        {
            TestRows.Row(product: "OIL", volume: "0.0", hours: "0"),
            TestRows.Row(product: "GAS", volume: "12.5", hours: null),
        };

        Assert.Single(_rule.Evaluate(rows, TestRows.Context()));
    }

    [Fact]
    public void Zero_volume_with_zero_hours_is_consistent()
    {
        Assert.Empty(_rule.Evaluate([TestRows.Row(volume: "0.0", hours: "0")], TestRows.Context()));
    }

    [Fact]
    public void Well_with_no_hours_reported_at_all_is_not_judged()
    {
        Assert.Empty(_rule.Evaluate([TestRows.Row(volume: "38.6", hours: null)], TestRows.Context()));
    }

    [Fact]
    public void Masked_hours_prevent_any_conclusion()
    {
        var rows = new[]
        {
            TestRows.Row(product: "OIL", volume: "206.9", hours: "***"),
            TestRows.Row(product: "GAS", volume: "21.8", hours: "0"),
        };

        Assert.Empty(_rule.Evaluate(rows, TestRows.Context()));
    }

    [Fact]
    public async Task Real_fixture_month_is_consistent()
    {
        Assert.Empty(_rule.Evaluate(await TestRows.FixtureDataRowsAsync(), TestRows.Context()));
    }
}

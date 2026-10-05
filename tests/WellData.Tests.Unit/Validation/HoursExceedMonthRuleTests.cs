using WellData.Core;
using WellData.Core.Validation.Rules;

namespace WellData.Tests.Unit.Validation;

public class HoursExceedMonthRuleTests
{
    private readonly HoursExceedMonthRule _rule = new();

    [Fact]
    public void Hours_equal_to_the_month_length_pass()
    {
        Assert.Empty(_rule.Evaluate([TestRows.Row(hours: "720")], TestRows.Context()));
    }

    [Fact]
    public void Hours_above_the_month_length_are_an_error()
    {
        var violation = Assert.Single(_rule.Evaluate([TestRows.Row(hours: "721")], TestRows.Context()));

        Assert.Equal("Hours", violation.Field);
        Assert.Contains("720", violation.Message);
    }

    [Fact]
    public void Limit_follows_the_month_so_744_is_valid_in_July_but_not_June()
    {
        var row = TestRows.Row(hours: "744");

        Assert.Empty(_rule.Evaluate([TestRows.Row(hours: "744", month: "2026-07")], TestRows.Context(new ProductionMonth(2026, 7))));
        Assert.Single(_rule.Evaluate([row], TestRows.Context()));
    }

    [Fact]
    public void A_well_is_judged_once_by_its_highest_hours_not_once_per_row()
    {
        var rows = new[]
        {
            TestRows.Row(product: "OIL", hours: "745"),
            TestRows.Row(product: "GAS", hours: null),
            TestRows.Row(product: "WATER", hours: "700"),
        };

        var violation = Assert.Single(_rule.Evaluate(rows, TestRows.Context()));

        Assert.Same(rows[0], violation.Row);
    }

    [Fact]
    public void Each_well_is_judged_separately()
    {
        var rows = new[]
        {
            TestRows.Row(hours: "800", fromTo: TestRows.Well),
            TestRows.Row(hours: "720", fromTo: TestRows.OtherWell),
        };

        var violation = Assert.Single(_rule.Evaluate(rows, TestRows.Context()));

        Assert.Equal(TestRows.Well, violation.Row.WellId);
    }

    [Fact]
    public void Masked_or_blank_hours_and_non_well_rows_are_ignored()
    {
        var rows = new[]
        {
            TestRows.Row(hours: "***"),
            TestRows.Row(hours: null, fromTo: TestRows.OtherWell),
            TestRows.Row(activity: "DISP", hours: "999", fromTo: "ABBT0040518", fromToType: "BT"),
        };

        Assert.Empty(_rule.Evaluate(rows, TestRows.Context()));
    }

    [Fact]
    public async Task Real_fixture_month_has_no_wells_over_the_limit()
    {
        Assert.Empty(_rule.Evaluate(await TestRows.FixtureDataRowsAsync(), TestRows.Context()));
    }
}

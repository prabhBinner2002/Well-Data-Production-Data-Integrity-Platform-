using WellData.Core.Validation.Rules;

namespace WellData.Tests.Unit.Validation;

public class FormatRuleTests
{
    private readonly FormatRule _rule = new();

    [Fact]
    public void Well_formed_row_passes()
    {
        Assert.Empty(_rule.Evaluate([TestRows.Row(hours: "720", energy: "0")], TestRows.Context()));
    }

    [Theory]
    [InlineData("thirty-eight")]
    [InlineData("38,6")]
    [InlineData("1.2.3")]
    public void Unparseable_volume_is_an_error(string volume)
    {
        var violation = Assert.Single(_rule.Evaluate([TestRows.Row(volume: volume)], TestRows.Context()));

        Assert.Equal("Volume", violation.Field);
        Assert.Equal(volume, violation.RawValue);
    }

    [Fact]
    public void Unparseable_hours_and_energy_are_each_reported()
    {
        var violations = _rule.Evaluate([TestRows.Row(hours: "7x0", energy: "n/a")], TestRows.Context()).ToList();

        Assert.Equal(["Energy", "Hours"], violations.Select(violation => violation.Field).Order());
    }

    [Theory]
    [InlineData("2026-13")]
    [InlineData("June 2026")]
    [InlineData("")]
    public void Unparseable_or_missing_month_is_an_error(string month)
    {
        var violation = Assert.Single(_rule.Evaluate([TestRows.Row(month: month)], TestRows.Context()));

        Assert.Equal("ProductionMonth", violation.Field);
    }

    [Fact]
    public void Month_that_differs_from_the_files_month_is_an_error()
    {
        var violation = Assert.Single(_rule.Evaluate([TestRows.Row(month: "2026-05")], TestRows.Context()));

        Assert.Contains("2026-06", violation.Message);
    }

    [Fact]
    public void Masked_and_blank_values_are_not_format_errors()
    {
        var rows = new[] { TestRows.Row(hours: "***"), TestRows.Row(volume: null), TestRows.Row(fromTo: "***", fromToType: null) };

        Assert.Empty(_rule.Evaluate(rows, TestRows.Context()));
    }

    [Fact]
    public void Identifier_longer_than_any_Petrinex_id_is_an_error()
    {
        var violation = Assert.Single(_rule.Evaluate([TestRows.Row(fromTo: new string('9', 21))], TestRows.Context()));

        Assert.Equal("FromToID", violation.Field);
    }

    [Fact]
    public async Task Real_fixture_month_has_no_format_errors()
    {
        Assert.Empty(_rule.Evaluate(await TestRows.FixtureDataRowsAsync(), TestRows.Context()));
    }
}

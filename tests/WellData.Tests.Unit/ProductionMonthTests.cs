using WellData.Core;

namespace WellData.Tests.Unit;

public class ProductionMonthTests
{
    [Theory]
    [InlineData("2026-06", 2026, 6)]
    [InlineData("1999-12", 1999, 12)]
    [InlineData("2024-01", 2024, 1)]
    public void Valid_text_parses_to_year_and_month(string text, int year, int month)
    {
        var parsed = ProductionMonth.Parse(text);

        Assert.Equal(year, parsed.Year);
        Assert.Equal(month, parsed.Month);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("2026-13")]
    [InlineData("2026-00")]
    [InlineData("2026/06")]
    [InlineData("26-06")]
    [InlineData("2026-6")]
    [InlineData("2026-06-01")]
    [InlineData("***")]
    public void Invalid_text_does_not_parse(string? text)
    {
        Assert.False(ProductionMonth.TryParse(text, out _));
    }

    [Fact]
    public void Parse_throws_FormatException_for_invalid_text()
    {
        Assert.Throws<FormatException>(() => ProductionMonth.Parse("June 2026"));
    }

    [Fact]
    public void ToString_round_trips_through_Parse()
    {
        var month = new ProductionMonth(2026, 6);

        Assert.Equal("2026-06", month.ToString());
        Assert.Equal(month, ProductionMonth.Parse(month.ToString()));
    }

    [Theory]
    [InlineData(2026, 6, 720)]
    [InlineData(2026, 7, 744)]
    [InlineData(2026, 2, 672)]
    [InlineData(2024, 2, 696)]
    public void HoursInMonth_accounts_for_month_length_and_leap_years(int year, int month, int expectedHours)
    {
        Assert.Equal(expectedHours, new ProductionMonth(year, month).HoursInMonth);
    }

    [Fact]
    public void AddMonths_crosses_year_boundaries()
    {
        Assert.Equal(new ProductionMonth(2026, 1), new ProductionMonth(2025, 12).AddMonths(1));
        Assert.Equal(new ProductionMonth(2025, 11), new ProductionMonth(2026, 1).AddMonths(-2));
    }

    [Fact]
    public void Months_order_chronologically()
    {
        Assert.True(new ProductionMonth(2025, 12).CompareTo(new ProductionMonth(2026, 1)) < 0);
    }
}

using WellData.Core;
using WellData.Core.Volumetric;

namespace WellData.Tests.Unit.Volumetric;

public class ParsedFieldTests
{
    [Theory]
    [InlineData("1884.5", 1884.5)]
    [InlineData("0.0", 0)]
    [InlineData("-12.345", -12.345)]
    [InlineData(" 42 ", 42)]
    public void Numeric_text_is_a_valid_decimal(string raw, double expected)
    {
        var field = ParsedField.Decimal(raw);

        Assert.Equal(FieldState.Valid, field.State);
        Assert.Equal((decimal)expected, field.Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Empty_text_is_blank_not_masked(string? raw)
    {
        var field = ParsedField.Decimal(raw);

        Assert.Equal(FieldState.Blank, field.State);
        Assert.Null(field.Value);
    }

    [Fact]
    public void Three_asterisks_are_masked_not_blank_or_invalid()
    {
        Assert.Equal(FieldState.Masked, ParsedField.Decimal("***").State);
        Assert.Equal(FieldState.Masked, ParsedField.Integer("***").State);
        Assert.True(ParsedField.IsMasked(" *** "));
        Assert.False(ParsedField.IsMasked("**"));
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("1,234.5")]
    [InlineData("1e5")]
    [InlineData("12.3.4")]
    [InlineData("99999999999.0")]
    public void Unreadable_or_oversized_numbers_are_invalid_and_keep_the_raw_text(string raw)
    {
        var field = ParsedField.Decimal(raw);

        Assert.Equal(FieldState.Invalid, field.State);
        Assert.Equal(raw, field.Raw);
    }

    [Fact]
    public void Hours_must_be_a_whole_number()
    {
        Assert.Equal(720, ParsedField.Integer("720").Value);
        Assert.Equal(FieldState.Invalid, ParsedField.Integer("720.5").State);
    }

    [Fact]
    public void Month_text_parses_to_a_production_month()
    {
        Assert.Equal(new ProductionMonth(2026, 6), ParsedField.Month("2026-06").Value);
        Assert.Equal(FieldState.Invalid, ParsedField.Month("2026-13").State);
        Assert.Equal(FieldState.Blank, ParsedField.Month("").State);
    }
}

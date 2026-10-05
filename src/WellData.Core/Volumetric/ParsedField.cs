using System.Globalization;

namespace WellData.Core.Volumetric;

/// <summary>A raw text value together with what it turned out to be: blank, masked, invalid, or a valid value.</summary>
public readonly record struct ParsedField<T>(FieldState State, T? Value, string Raw)
    where T : struct
{
    public bool IsValid => State == FieldState.Valid;
}

public static class ParsedField
{
    public const string Mask = "***";

    // decimal(13,3) holds at most ten digits before the decimal point.
    private const decimal MaxMagnitude = 9_999_999_999.999m;

    public static bool IsMasked(string? raw) => raw?.Trim() == Mask;

    public static ParsedField<decimal> Decimal(string? raw) =>
        Parse<decimal>(raw, text =>
            decimal.TryParse(text, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var value) &&
            Math.Abs(value) <= MaxMagnitude
                ? value
                : null);

    public static ParsedField<int> Integer(string? raw) =>
        Parse<int>(raw, text =>
            int.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var value)
                ? value
                : null);

    public static ParsedField<ProductionMonth> Month(string? raw) =>
        Parse<ProductionMonth>(raw, text => ProductionMonth.TryParse(text, out var value) ? value : null);

    private static ParsedField<T> Parse<T>(string? raw, Func<string, T?> tryParse)
        where T : struct
    {
        var text = raw?.Trim() ?? string.Empty;

        if (text.Length == 0)
        {
            return new ParsedField<T>(FieldState.Blank, null, text);
        }

        if (text == Mask)
        {
            return new ParsedField<T>(FieldState.Masked, null, text);
        }

        return tryParse(text) is { } value
            ? new ParsedField<T>(FieldState.Valid, value, text)
            : new ParsedField<T>(FieldState.Invalid, null, text);
    }
}

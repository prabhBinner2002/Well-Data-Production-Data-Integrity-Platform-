using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace WellData.Core;

/// <summary>A Petrinex reporting period: one calendar month, written as YYYY-MM.</summary>
public readonly record struct ProductionMonth : IComparable<ProductionMonth>
{
    public ProductionMonth(int year, int month)
    {
        if (year is < 1900 or > 2200)
        {
            throw new ArgumentOutOfRangeException(nameof(year), year, "Year must be between 1900 and 2200.");
        }

        if (month is < 1 or > 12)
        {
            throw new ArgumentOutOfRangeException(nameof(month), month, "Month must be between 1 and 12.");
        }

        Year = year;
        Month = month;
    }

    public int Year { get; }

    public int Month { get; }

    public DateOnly FirstDay => new(Year, Month, 1);

    public int HoursInMonth => DateTime.DaysInMonth(Year, Month) * 24;

    public static ProductionMonth FromDate(DateOnly date) => new(date.Year, date.Month);

    public static ProductionMonth Parse(string text) =>
        TryParse(text, out var month)
            ? month
            : throw new FormatException($"'{text}' is not a production month in YYYY-MM format.");

    public static bool TryParse([NotNullWhen(true)] string? text, out ProductionMonth month)
    {
        month = default;

        if (text is null || text.Length != 7 || text[4] != '-')
        {
            return false;
        }

        if (!int.TryParse(text.AsSpan(0, 4), NumberStyles.None, CultureInfo.InvariantCulture, out var year) ||
            !int.TryParse(text.AsSpan(5, 2), NumberStyles.None, CultureInfo.InvariantCulture, out var monthNumber) ||
            year is < 1900 or > 2200 ||
            monthNumber is < 1 or > 12)
        {
            return false;
        }

        month = new ProductionMonth(year, monthNumber);
        return true;
    }

    public ProductionMonth AddMonths(int months) => FromDate(FirstDay.AddMonths(months));

    public int CompareTo(ProductionMonth other) => FirstDay.CompareTo(other.FirstDay);

    public static bool operator <(ProductionMonth left, ProductionMonth right) => left.CompareTo(right) < 0;

    public static bool operator >(ProductionMonth left, ProductionMonth right) => left.CompareTo(right) > 0;

    public static bool operator <=(ProductionMonth left, ProductionMonth right) => left.CompareTo(right) <= 0;

    public static bool operator >=(ProductionMonth left, ProductionMonth right) => left.CompareTo(right) >= 0;

    public override string ToString() => $"{Year:D4}-{Month:D2}";
}

using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;

namespace WellData.Infrastructure.Ingestion;

/// <summary>Shared CSV settings and field helpers for Petrinex files.</summary>
internal static class PetrinexCsv
{
    private static readonly CsvConfiguration Configuration = new(CultureInfo.InvariantCulture)
    {
        TrimOptions = TrimOptions.Trim,
        MissingFieldFound = null,
        BadDataFound = null,
    };

    public static CsvReader CreateReader(TextReader reader) => new(reader, Configuration);

    public static string Text(this CsvReader csv, string column) => csv.GetField(column)?.Trim() ?? string.Empty;

    public static string? OptionalText(this CsvReader csv, string column, int maxLength)
    {
        var value = csv.Text(column);
        return value.Length == 0 ? null : Limit(value, maxLength);
    }

    /// <summary>Petrinex names occasionally exceed the documented length, so cap them rather than fail the load.</summary>
    public static string Limit(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength];
}

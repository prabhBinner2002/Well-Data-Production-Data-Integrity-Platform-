using WellData.Core;
using WellData.Core.Entities;
using WellData.Core.Validation;
using WellData.Core.Volumetric;
using WellData.Infrastructure.Ingestion.Volumetric;
using WellData.Infrastructure.Persistence.Seed;

namespace WellData.Tests.Unit.Validation;

/// <summary>Builds rows shaped like the real fixture rows, with one value changed per test.</summary>
internal static class TestRows
{
    public const string Well = "ABWI100013604905W500";
    public const string OtherWell = "ABWI100023604905W500";

    private static int _nextRowNumber = 2;

    public static readonly ProductionMonth June2026 = new(2026, 6);

    public static ValidationContext Context(ProductionMonth? month = null) => new(
        month ?? June2026,
        ReferenceData.ActivityCodes.ToDictionary(activity => activity.Code),
        ReferenceData.ProductCodes.Select(product => product.Code).ToHashSet());

    public static IReadOnlyDictionary<string, DataQualityRule> Catalog() =>
        RuleCatalog.FileRules.ToDictionary(
            rule => rule.Code,
            rule => new DataQualityRule { Code = rule.Code, Description = rule.Description, Severity = rule.Severity, IsEnabled = true });

    public static ParsedVolumetricRow Row(
        string activity = "PROD",
        string product = "OIL",
        string? volume = "38.6",
        string? hours = null,
        string? fromTo = Well,
        string? fromToType = "WI",
        string? month = "2026-06",
        string? energy = null,
        string facility = "ABBT0131180") =>
        new(new StagedVolumetricRow
        {
            RowNumber = _nextRowNumber++,
            ProductionMonth = month,
            OperatorBaId = "0XZ4",
            ReportingFacilityId = facility,
            ActivityId = activity,
            ProductId = product,
            FromToId = fromTo,
            FromToIdType = fromToType,
            Volume = volume,
            Energy = energy,
            Hours = hours,
        });

    public static async Task<List<ParsedVolumetricRow>> FixtureDataRowsAsync(string fixture = FixtureFiles.Volumetric)
    {
        using var reader = FixtureFiles.Open(fixture);
        var rows = new List<ParsedVolumetricRow>();
        await foreach (var staged in new VolumetricFileReader().ReadAsync(reader, CancellationToken.None))
        {
            var row = new ParsedVolumetricRow(staged);
            if (!row.IsProrationHeader)
            {
                rows.Add(row);
            }
        }

        return rows;
    }
}

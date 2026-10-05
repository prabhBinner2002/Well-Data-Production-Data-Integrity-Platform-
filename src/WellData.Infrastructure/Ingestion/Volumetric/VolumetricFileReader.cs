using System.Runtime.CompilerServices;
using CsvHelper;
using WellData.Core.Entities;
using WellData.Infrastructure.Persistence.Configurations;

namespace WellData.Infrastructure.Ingestion.Volumetric;

/// <summary>Streams a Petrinex volumetric CSV as raw staged rows. Nothing is interpreted or validated here.</summary>
public sealed class VolumetricFileReader
{
    private static readonly string[] RequiredColumns =
    [
        "ProductionMonth", "OperatorBAID", "ReportingFacilityID", "ActivityID", "ProductID",
        "FromToID", "FromToIDType", "Volume", "Energy", "Hours",
    ];

    public async IAsyncEnumerable<StagedVolumetricRow> ReadAsync(
        TextReader reader,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        using var csv = PetrinexCsv.CreateReader(reader);
        await csv.ReadAsync();
        csv.ReadHeader();
        EnsureVolumetricHeader(csv);

        while (await csv.ReadAsync())
        {
            cancellationToken.ThrowIfCancellationRequested();

            yield return new StagedVolumetricRow
            {
                RowNumber = csv.Parser.RawRow,
                ProductionMonth = Text(csv, "ProductionMonth"),
                OperatorBaId = Text(csv, "OperatorBAID"),
                OperatorName = Name(csv, "OperatorName"),
                ReportingFacilityId = Text(csv, "ReportingFacilityID"),
                ReportingFacilityType = Text(csv, "ReportingFacilityType"),
                ReportingFacilityName = Name(csv, "ReportingFacilityName"),
                SubmissionDate = Text(csv, "SubmissionDate"),
                ActivityId = Text(csv, "ActivityID"),
                ProductId = Text(csv, "ProductID"),
                FromToId = Text(csv, "FromToID"),
                FromToIdType = Text(csv, "FromToIDType"),
                Volume = Text(csv, "Volume"),
                Energy = Text(csv, "Energy"),
                Hours = Text(csv, "Hours"),
                ProrationProduct = Text(csv, "ProrationProduct"),
                ProrationFactor = Text(csv, "ProrationFactor"),
            };
        }
    }

    private static void EnsureVolumetricHeader(CsvReader csv)
    {
        var header = csv.HeaderRecord ?? [];
        var missing = RequiredColumns.Except(header, StringComparer.OrdinalIgnoreCase).ToList();

        if (missing.Count > 0)
        {
            throw new InvalidDataException($"Not a Petrinex volumetric file. Missing columns: {string.Join(", ", missing)}.");
        }
    }

    private static string? Text(CsvReader csv, string column) =>
        csv.OptionalText(column, StagedVolumetricRowConfiguration.TextLength);

    private static string? Name(CsvReader csv, string column) =>
        csv.OptionalText(column, StagedVolumetricRowConfiguration.NameLength);
}

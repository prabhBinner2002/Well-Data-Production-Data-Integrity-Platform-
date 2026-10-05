using System.Runtime.CompilerServices;
using WellData.Core;

namespace WellData.Infrastructure.Ingestion.Master;

public sealed record WellRow(
    string WellId,
    string Uwi,
    string Name,
    string? LicenceNumber,
    string? LicenseeBaId,
    string? Field,
    string? StatusFluid,
    string? StatusMode,
    string? LicenceStatus,
    bool IsConfidential,
    string? LinkedFacilityOperatorBaId);

public sealed class WellFileParser : IMasterFileParser<WellRow>
{
    private const string NonConfidential = "NON-CONFIDENTIAL";

    public async IAsyncEnumerable<WellRow> ParseAsync(
        TextReader reader,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        using var csv = PetrinexCsv.CreateReader(reader);
        await csv.ReadAsync();
        csv.ReadHeader();

        while (await csv.ReadAsync())
        {
            cancellationToken.ThrowIfCancellationRequested();

            var wellId = csv.Text("WellID");
            if (wellId.Length is 0 or > 20)
            {
                continue;
            }

            var confidentialType = csv.Text("ConfidentialType");

            yield return new WellRow(
                wellId,
                UwiFormatter.Format(csv.Text("WellIdentifier")),
                PetrinexCsv.Limit(csv.Text("WellName"), 100),
                csv.OptionalText("LicenceNumber", 10),
                csv.OptionalText("LicenseeID", 4),
                csv.OptionalText("FieldName", 50),
                csv.OptionalText("WellStatusFluid", 20),
                csv.OptionalText("WellStatusMode", 20),
                csv.OptionalText("LicenceStatus", 20),
                confidentialType.Length > 0 && confidentialType != NonConfidential,
                csv.OptionalText("LinkedFacilityOperatorBAID", 4));
        }
    }
}

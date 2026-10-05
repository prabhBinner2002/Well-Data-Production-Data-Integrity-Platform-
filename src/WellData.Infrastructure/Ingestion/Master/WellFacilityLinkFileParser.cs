using System.Globalization;
using System.Runtime.CompilerServices;

namespace WellData.Infrastructure.Ingestion.Master;

public sealed record WellFacilityLinkRow(string WellId, string FacilityId, DateOnly StartDate, string? LinkedFacilityOperatorBaId);

public sealed class WellFacilityLinkFileParser : IMasterFileParser<WellFacilityLinkRow>
{
    public async IAsyncEnumerable<WellFacilityLinkRow> ParseAsync(
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
            var facilityId = csv.Text("LinkedFacilityID");
            var hasStartDate = DateOnly.TryParseExact(
                csv.Text("LinkedStartDate"),
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var startDate);

            if (wellId.Length is 0 or > 20 || facilityId.Length is 0 or > 20 || !hasStartDate)
            {
                continue;
            }

            yield return new WellFacilityLinkRow(wellId, facilityId, startDate, csv.OptionalText("LinkedFacilityOperatorBAID", 4));
        }
    }
}

using System.Runtime.CompilerServices;

namespace WellData.Infrastructure.Ingestion.Master;

public sealed record FacilityRow(
    string FacilityId,
    string FacilityType,
    string Name,
    string? SubType,
    string? SubTypeDescription,
    string? Location,
    string? OperationalStatus,
    string OperatorBaId);

public sealed class FacilityFileParser : IMasterFileParser<FacilityRow>
{
    public async IAsyncEnumerable<FacilityRow> ParseAsync(
        TextReader reader,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        using var csv = PetrinexCsv.CreateReader(reader);
        await csv.ReadAsync();
        csv.ReadHeader();

        while (await csv.ReadAsync())
        {
            cancellationToken.ThrowIfCancellationRequested();

            var facilityId = csv.Text("FacilityID");
            var operatorBaId = csv.Text("OperatorBAID");
            if (facilityId.Length is 0 or > 20 || operatorBaId.Length is 0 or > 4)
            {
                continue;
            }

            yield return new FacilityRow(
                facilityId,
                PetrinexCsv.Limit(csv.Text("FacilityType"), 2),
                PetrinexCsv.Limit(csv.Text("FacilityName"), 100),
                csv.OptionalText("FacilitySubType", 3),
                csv.OptionalText("FacilitySubTypeDesc", 100),
                csv.OptionalText("FacilityLocation", 20),
                csv.OptionalText("FacilityOperationalStatus", 30),
                operatorBaId);
        }
    }
}

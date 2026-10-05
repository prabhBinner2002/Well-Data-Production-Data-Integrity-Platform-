using System.Runtime.CompilerServices;

namespace WellData.Infrastructure.Ingestion.Master;

public sealed record BusinessAssociateRow(string BaId, string LegalName, string? AbbreviatedName, string? CorporateStatus);

public sealed class BusinessAssociateFileParser : IMasterFileParser<BusinessAssociateRow>
{
    public async IAsyncEnumerable<BusinessAssociateRow> ParseAsync(
        TextReader reader,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        using var csv = PetrinexCsv.CreateReader(reader);
        await csv.ReadAsync();
        csv.ReadHeader();

        while (await csv.ReadAsync())
        {
            cancellationToken.ThrowIfCancellationRequested();

            var baId = csv.Text("BAIdentifier");
            if (baId.Length is 0 or > 4)
            {
                continue;
            }

            yield return new BusinessAssociateRow(
                baId,
                PetrinexCsv.Limit(csv.Text("BALegalName"), 150),
                csv.OptionalText("BAAbbreviatedName", 50),
                csv.OptionalText("BACorporateStatus", 30));
        }
    }
}

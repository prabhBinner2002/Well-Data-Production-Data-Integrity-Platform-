namespace WellData.Infrastructure.Ingestion.Master;

/// <summary>Streams the rows of one Petrinex infrastructure file. Rows without a usable key are dropped.</summary>
public interface IMasterFileParser<out TRow>
{
    IAsyncEnumerable<TRow> ParseAsync(TextReader reader, CancellationToken cancellationToken);
}

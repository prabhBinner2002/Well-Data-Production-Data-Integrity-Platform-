using Microsoft.Extensions.Logging;
using WellData.Core;
using WellData.Infrastructure.Petrinex;

namespace WellData.Infrastructure.Ingestion.Volumetric;

/// <summary>Loads the most recent published months, oldest first.</summary>
public sealed class BackfillService(
    IPetrinexClient petrinexClient,
    VolumetricIngestionService ingestion,
    TimeProvider timeProvider,
    ILogger<BackfillService> logger)
{
    // Petrinex publishes a month roughly two months after it ends, so the newest few months are usually missing.
    private const int UnpublishedMonthAllowance = 6;

    public Task<IReadOnlyList<ImportResult>> BackfillAsync(int months, CancellationToken cancellationToken) =>
        BackfillAsync(months, reloadUnchanged: false, cancellationToken);

    /// <param name="reloadUnchanged">Reload months whose file is already loaded, for use after the operator subset changes.</param>
    public async Task<IReadOnlyList<ImportResult>> BackfillAsync(int months, bool reloadUnchanged, CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(months, 1);

        var published = await FindPublishedMonthsAsync(months, cancellationToken);
        var results = new List<ImportResult>();

        // Oldest first, so each month's gap, spike, and outlier rules see the history before it.
        foreach (var month in published.Order())
        {
            results.Add(await ingestion.IngestMonthAsync(month, reloadUnchanged, cancellationToken));
        }

        return results;
    }

    /// <summary>Walks back from last month, fetching each file into the cache, until enough published months are found.</summary>
    private async Task<List<ProductionMonth>> FindPublishedMonthsAsync(int months, CancellationToken cancellationToken)
    {
        var published = new List<ProductionMonth>();
        var month = ProductionMonth.FromDate(DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime)).AddMonths(-1);

        for (var attempt = 0; published.Count < months && attempt < months + UnpublishedMonthAllowance; attempt++)
        {
            try
            {
                await petrinexClient.GetVolumetricFileAsync(month, cancellationToken);
                published.Add(month);
            }
            catch (PetrinexFileNotAvailableException)
            {
                logger.LogInformation("{Month} is not published yet", month);
            }

            month = month.AddMonths(-1);
        }

        return published;
    }
}

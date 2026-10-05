using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WellData.Core;
using WellData.Infrastructure.Ingestion.Volumetric;
using WellData.Infrastructure.Petrinex;

namespace WellData.Infrastructure.Scheduling;

/// <summary>
/// One scheduled pass: looks at the months in the lookback window and ingests any that are new or whose
/// file changed. A month whose file hash is already loaded comes back as Unchanged and costs no database work.
/// </summary>
public sealed class ScheduledMonthChecker(
    VolumetricIngestionService ingestion,
    IOptions<IngestionSchedulerOptions> options,
    TimeProvider timeProvider,
    ILogger<ScheduledMonthChecker> logger)
{
    public async Task<IReadOnlyList<ImportResult>> CheckAsync(CancellationToken cancellationToken)
    {
        var results = new List<ImportResult>();

        // Oldest first, so the month-over-month rules of each month can see the month before it.
        foreach (var month in MonthsToCheck())
        {
            try
            {
                results.Add(await ingestion.IngestMonthAsync(month, cancellationToken));
            }
            catch (PetrinexFileNotAvailableException)
            {
                logger.LogDebug("{Month} is not published yet", month);
            }
        }

        logger.LogInformation(
            "Scheduled check finished: {Loaded} loaded, {Unchanged} unchanged, {Failed} failed",
            results.Count(result => result.Outcome == ImportOutcome.Loaded),
            results.Count(result => result.Outcome == ImportOutcome.Unchanged),
            results.Count(result => result.Outcome == ImportOutcome.Failed));

        return results;
    }

    /// <summary>The complete months before the current one, limited to the lookback window.</summary>
    public IEnumerable<ProductionMonth> MonthsToCheck()
    {
        var currentMonth = ProductionMonth.FromDate(DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime));

        return Enumerable
            .Range(1, options.Value.LookbackMonths)
            .Select(monthsBack => currentMonth.AddMonths(-monthsBack))
            .Order();
    }
}

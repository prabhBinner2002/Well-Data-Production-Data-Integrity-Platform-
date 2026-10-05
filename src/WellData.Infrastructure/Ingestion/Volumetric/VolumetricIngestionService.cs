using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WellData.Core;
using WellData.Core.Entities;
using WellData.Infrastructure.Periods;
using WellData.Infrastructure.Persistence;
using WellData.Infrastructure.Petrinex;
using WellData.Infrastructure.Validation;

namespace WellData.Infrastructure.Ingestion.Volumetric;

/// <summary>Loads one production month: fetch the file, stage the subset's raw rows, promote them, then run the SQL rules.</summary>
public sealed class VolumetricIngestionService(
    IPetrinexClient petrinexClient,
    SourceFileRegistry sourceFiles,
    StagingBulkLoader stagingLoader,
    VolumetricPromoter promoter,
    SqlRuleRunner sqlRules,
    PeriodGuard periodGuard,
    OperatorSubset subset,
    WellDataDbContext context,
    TimeProvider timeProvider,
    ILogger<VolumetricIngestionService> logger)
{
    private const int MaxErrorLength = 2000;

    /// <exception cref="PetrinexFileNotAvailableException">Petrinex has not published the month.</exception>
    public Task<ImportResult> IngestMonthAsync(ProductionMonth month, CancellationToken cancellationToken) =>
        IngestMonthAsync(month, reloadUnchanged: false, cancellationToken);

    /// <param name="reloadUnchanged">
    /// Load the month even when its file hash is already loaded. Needed after the operator subset changes,
    /// because the same file then yields different rows.
    /// </param>
    /// <exception cref="PetrinexFileNotAvailableException">Petrinex has not published the month.</exception>
    public async Task<ImportResult> IngestMonthAsync(ProductionMonth month, bool reloadUnchanged, CancellationToken cancellationToken)
    {
        if (await periodGuard.IsClosedAsync(month, cancellationToken))
        {
            logger.LogWarning("{Month} is closed and was not imported", month);
            return new ImportResult(month, ImportOutcome.PeriodClosed, Run: null);
        }

        var file = await petrinexClient.GetVolumetricFileAsync(month, cancellationToken);
        var sourceFile = await sourceFiles.GetOrRegisterAsync(file, cancellationToken);

        if (sourceFile.Status == SourceFileStatus.Loaded && !reloadUnchanged)
        {
            logger.LogInformation("{Month} is unchanged (hash {Hash}), skipping", month, sourceFile.Sha256);
            return new ImportResult(month, ImportOutcome.Unchanged, Run: null);
        }

        var run = new ImportRun
        {
            SourceFileId = sourceFile.Id,
            ProductionMonth = month,
            StartedAt = timeProvider.GetUtcNow().UtcDateTime,
            Status = ImportRunStatus.Running,
        };
        context.ImportRuns.Add(run);
        await context.SaveChangesAsync(cancellationToken);

        try
        {
            run.RowsStaged = await StageAsync(file, sourceFile.Id, cancellationToken);

            var promotion = await promoter.PromoteAsync(sourceFile, month, cancellationToken);
            run.RowsLoaded = promotion.RowsLoaded;
            run.RowsRejected = promotion.RowsRejected;
            run.RowsSkipped = promotion.RowsSkipped;

            await RunSqlRulesAsync(month, cancellationToken);

            run.Status = ImportRunStatus.Succeeded;
            run.CompletedAt = timeProvider.GetUtcNow().UtcDateTime;
            await context.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                "{Month}: staged {Staged}, loaded {Loaded}, rejected {Rejected}, skipped {Skipped}",
                month,
                run.RowsStaged,
                run.RowsLoaded,
                run.RowsRejected,
                run.RowsSkipped);

            return new ImportResult(month, ImportOutcome.Loaded, run);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "Import of {Month} failed", month);
            await RecordFailureAsync(run, sourceFile.Id, exception, cancellationToken);
            return new ImportResult(month, ImportOutcome.Failed, run);
        }
    }

    /// <summary>
    /// The gap and spike rules for the following month compare against this month, so when that month
    /// is already loaded (a backfill, or a restatement of an older month) its findings are refreshed too.
    /// </summary>
    private async Task RunSqlRulesAsync(ProductionMonth month, CancellationToken cancellationToken)
    {
        await sqlRules.RunAsync(month, cancellationToken);

        var nextMonth = month.AddMonths(1);
        var nextMonthIsLoaded = await context.VolumetricRecords.AnyAsync(record => record.ProductionMonth == nextMonth, cancellationToken);

        // A closed month keeps the issues it was approved with.
        if (nextMonthIsLoaded && !await periodGuard.IsClosedAsync(nextMonth, cancellationToken))
        {
            await sqlRules.RunAsync(nextMonth, cancellationToken);
        }
    }

    private async Task<int> StageAsync(PetrinexFile file, int sourceFileId, CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(file.LocalPath);
        var subsetRows = new VolumetricFileReader()
            .ReadAsync(reader, cancellationToken)
            .Where(row => subset.Includes(row.OperatorBaId));

        return await stagingLoader.LoadAsync(sourceFileId, subsetRows, cancellationToken);
    }

    /// <summary>
    /// The promotion transaction has already rolled back. Tracked entities may hold values that were
    /// never committed, so the failure is written with direct updates instead of SaveChanges.
    /// </summary>
    private async Task RecordFailureAsync(ImportRun run, int sourceFileId, Exception exception, CancellationToken cancellationToken)
    {
        context.ChangeTracker.Clear();

        var completedAt = timeProvider.GetUtcNow().UtcDateTime;
        var message = exception.Message.Length <= MaxErrorLength ? exception.Message : exception.Message[..MaxErrorLength];
        var rowsStaged = run.RowsStaged;

        await context.ImportRuns
            .Where(candidate => candidate.Id == run.Id)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(candidate => candidate.Status, ImportRunStatus.Failed)
                    .SetProperty(candidate => candidate.ErrorMessage, message)
                    .SetProperty(candidate => candidate.RowsStaged, rowsStaged)
                    .SetProperty(candidate => candidate.CompletedAt, completedAt),
                cancellationToken);

        await context.SourceFiles
            .Where(file => file.Id == sourceFileId)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(file => file.Status, SourceFileStatus.Failed),
                cancellationToken);

        run.Status = ImportRunStatus.Failed;
        run.ErrorMessage = message;
        run.CompletedAt = completedAt;
        run.RowsLoaded = 0;
        run.RowsRejected = 0;
        run.RowsSkipped = 0;
    }
}

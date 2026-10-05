using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using WellData.Api.Dtos;
using WellData.Core;
using WellData.Core.Entities;
using WellData.Infrastructure.Ingestion.Volumetric;
using WellData.Infrastructure.Persistence;

namespace WellData.Api.Services;

public sealed class ImportService(WellDataDbContext context, VolumetricIngestionService ingestion)
{
    private static readonly Expression<Func<ImportRun, ImportRunDto>> ToDto = run =>
        new ImportRunDto(
            run.Id,
            run.ProductionMonth,
            run.Status,
            run.StartedAt,
            run.CompletedAt,
            run.RowsStaged,
            run.RowsLoaded,
            run.RowsRejected,
            run.RowsSkipped,
            run.ErrorMessage,
            run.SourceFile.FileName,
            run.SourceFile.Sha256,
            run.SourceFile.Status);

    public Task<PagedResult<ImportRunDto>> ListAsync(PageQuery page, CancellationToken cancellationToken) =>
        context.ImportRuns
            .AsNoTracking()
            .OrderByDescending(run => run.StartedAt)
            .ThenByDescending(run => run.Id)
            .Select(ToDto)
            .ToPagedResultAsync(page, cancellationToken);

    /// <exception cref="WellData.Infrastructure.Petrinex.PetrinexFileNotAvailableException">Petrinex has not published the month.</exception>
    public async Task<ImportResultDto> ImportAsync(ProductionMonth month, CancellationToken cancellationToken)
    {
        var result = await ingestion.IngestMonthAsync(month, cancellationToken);

        var run = result.Run is null
            ? null
            : await context.ImportRuns
                .AsNoTracking()
                .Where(candidate => candidate.Id == result.Run.Id)
                .Select(ToDto)
                .SingleAsync(cancellationToken);

        return new ImportResultDto(result.Month, result.Outcome, run);
    }
}

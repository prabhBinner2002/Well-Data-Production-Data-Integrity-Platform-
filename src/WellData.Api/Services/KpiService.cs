using Microsoft.EntityFrameworkCore;
using WellData.Api.Dtos;
using WellData.Core.Entities;
using WellData.Infrastructure.Persistence;

namespace WellData.Api.Services;

public sealed class KpiService(WellDataDbContext context)
{
    /// <summary>Share of each month's data rows that passed validation, taken from the load of the file currently in use.</summary>
    public async Task<List<AcceptanceRateDto>> GetAcceptanceRatesAsync(CancellationToken cancellationToken)
    {
        var runs = await context.ImportRuns
            .AsNoTracking()
            .Where(run => run.Status == ImportRunStatus.Succeeded)
            .Where(run => run.SourceFile.Status == SourceFileStatus.Loaded)
            .GroupBy(run => run.ProductionMonth)
            .Select(monthRuns => monthRuns.OrderByDescending(run => run.StartedAt).First())
            .ToListAsync(cancellationToken);

        return runs
            .OrderBy(run => run.ProductionMonth)
            .Select(run =>
            {
                var dataRows = run.RowsStaged - run.RowsSkipped;
                var rate = dataRows == 0 ? 0 : Math.Round((decimal)run.RowsLoaded / dataRows, 4);
                return new AcceptanceRateDto(run.ProductionMonth, dataRows, run.RowsLoaded, run.RowsRejected, rate);
            })
            .ToList();
    }

    public Task<List<IssueCountDto>> GetIssueCountsAsync(CancellationToken cancellationToken) =>
        context.DataQualityIssues
            .AsNoTracking()
            .GroupBy(issue => new { issue.ProductionMonth, issue.RuleCode, issue.Rule.Severity })
            .OrderBy(group => group.Key.ProductionMonth)
            .ThenBy(group => group.Key.RuleCode)
            .Select(group => new IssueCountDto(group.Key.ProductionMonth, group.Key.RuleCode, group.Key.Severity, group.Count()))
            .ToListAsync(cancellationToken);

    public Task<List<ProductionTotalDto>> GetProductionTotalsAsync(CancellationToken cancellationToken) =>
        context.MonthlyWellProduction
            .AsNoTracking()
            .GroupBy(row => row.ProductionMonth)
            .OrderBy(group => group.Key)
            .Select(group => new ProductionTotalDto(
                group.Key,
                group.Count(),
                group.Sum(row => row.OilVolume ?? 0),
                group.Sum(row => row.GasVolume ?? 0),
                group.Sum(row => row.WaterVolume ?? 0),
                group.Sum(row => row.CondensateVolume ?? 0)))
            .ToListAsync(cancellationToken);
}

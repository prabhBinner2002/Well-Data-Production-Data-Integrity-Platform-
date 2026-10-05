using Microsoft.EntityFrameworkCore;
using WellData.Core;
using WellData.Core.Entities;
using WellData.Infrastructure.Auditing;
using WellData.Infrastructure.Persistence;

namespace WellData.Infrastructure.Periods;

public enum ClosePeriodOutcome
{
    Closed,
    AlreadyClosed,

    /// <summary>Nothing has been loaded for the month, so there is nothing to approve.</summary>
    NoData,
}

public sealed record ClosePeriodResult(ClosePeriodOutcome Outcome, ProductionPeriod? Period = null);

public sealed record PeriodSummary(ProductionMonth ProductionMonth, PeriodStatus Status, string? ClosedBy, DateTime? ClosedAt);

public sealed class PeriodService(WellDataDbContext context, ICurrentUser currentUser, TimeProvider timeProvider)
{
    /// <summary>Every month that has loaded data, newest first, with its close state.</summary>
    public async Task<List<PeriodSummary>> ListAsync(CancellationToken cancellationToken)
    {
        var loadedMonths = await context.VolumetricRecords
            .AsNoTracking()
            .Select(record => record.ProductionMonth)
            .Distinct()
            .ToListAsync(cancellationToken);

        var periods = await context.ProductionPeriods
            .AsNoTracking()
            .ToDictionaryAsync(period => period.ProductionMonth, cancellationToken);

        return loadedMonths
            .OrderDescending()
            .Select(month => periods.TryGetValue(month, out var period)
                ? new PeriodSummary(month, period.Status, period.ClosedBy, period.ClosedAt)
                : new PeriodSummary(month, PeriodStatus.Open, ClosedBy: null, ClosedAt: null))
            .ToList();
    }

    public async Task<ClosePeriodResult> CloseAsync(ProductionMonth month, CancellationToken cancellationToken)
    {
        var existing = await context.ProductionPeriods.SingleOrDefaultAsync(period => period.ProductionMonth == month, cancellationToken);

        if (existing?.Status == PeriodStatus.Closed)
        {
            return new ClosePeriodResult(ClosePeriodOutcome.AlreadyClosed, existing);
        }

        if (!await context.VolumetricRecords.AnyAsync(record => record.ProductionMonth == month, cancellationToken))
        {
            return new ClosePeriodResult(ClosePeriodOutcome.NoData);
        }

        var period = existing ?? context.ProductionPeriods.Add(new ProductionPeriod { ProductionMonth = month }).Entity;
        period.Status = PeriodStatus.Closed;
        period.ClosedBy = currentUser.Name;
        period.ClosedAt = timeProvider.GetUtcNow().UtcDateTime;

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException) when (existing is null)
        {
            // Another request inserted the period first and the unique index rejected this one.
            return new ClosePeriodResult(ClosePeriodOutcome.AlreadyClosed);
        }

        return new ClosePeriodResult(ClosePeriodOutcome.Closed, period);
    }
}

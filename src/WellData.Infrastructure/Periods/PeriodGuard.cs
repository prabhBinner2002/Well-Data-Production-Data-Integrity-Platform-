using Microsoft.EntityFrameworkCore;
using WellData.Core;
using WellData.Core.Entities;
using WellData.Infrastructure.Persistence;

namespace WellData.Infrastructure.Periods;

/// <summary>Answers whether a production month has been closed and must not change.</summary>
public sealed class PeriodGuard(WellDataDbContext context)
{
    public Task<bool> IsClosedAsync(ProductionMonth month, CancellationToken cancellationToken) =>
        context.ProductionPeriods
            .AsNoTracking()
            .AnyAsync(period => period.ProductionMonth == month && period.Status == PeriodStatus.Closed, cancellationToken);
}

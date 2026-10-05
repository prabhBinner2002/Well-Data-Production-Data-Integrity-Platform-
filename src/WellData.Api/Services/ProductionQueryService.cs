using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using WellData.Api.Dtos;
using WellData.Core.Entities;
using WellData.Infrastructure.Persistence;

namespace WellData.Api.Services;

/// <summary>Reads monthly well production from vw_MonthlyWellProduction.</summary>
public sealed class ProductionQueryService(WellDataDbContext context)
{
    public static readonly Expression<Func<MonthlyWellProduction, MonthlyProductionDto>> ToDto = row =>
        new MonthlyProductionDto(
            row.ProductionMonth,
            row.WellId,
            row.FacilityId,
            row.OperatorBaId,
            row.OilVolume,
            row.GasVolume,
            row.WaterVolume,
            row.CondensateVolume,
            row.ProducingHours,
            row.IsMasked);

    public Task<PagedResult<MonthlyProductionDto>> ListAsync(ProductionFilter filter, PageQuery page, CancellationToken cancellationToken)
    {
        var production = context.MonthlyWellProduction.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(filter.WellId))
        {
            production = production.Where(row => row.WellId == filter.WellId);
        }

        if (!string.IsNullOrWhiteSpace(filter.OperatorBaId))
        {
            production = production.Where(row => row.OperatorBaId == filter.OperatorBaId);
        }

        if (filter.FromMonth is { } fromMonth)
        {
            production = production.Where(row => row.ProductionMonth >= fromMonth);
        }

        if (filter.ToMonth is { } toMonth)
        {
            production = production.Where(row => row.ProductionMonth <= toMonth);
        }

        return production
            .OrderBy(row => row.ProductionMonth)
            .ThenBy(row => row.WellId)
            .Select(ToDto)
            .ToPagedResultAsync(page, cancellationToken);
    }
}

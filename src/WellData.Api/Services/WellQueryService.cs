using Microsoft.EntityFrameworkCore;
using WellData.Api.Dtos;
using WellData.Infrastructure.Persistence;

namespace WellData.Api.Services;

public sealed class WellQueryService(WellDataDbContext context)
{
    public Task<PagedResult<WellSummaryDto>> ListAsync(WellFilter filter, PageQuery page, CancellationToken cancellationToken)
    {
        var wells = context.Wells.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(filter.OperatorBaId))
        {
            wells = wells.Where(well => well.LicenseeBaId == filter.OperatorBaId);
        }

        if (!string.IsNullOrWhiteSpace(filter.FacilityId))
        {
            wells = wells.Where(well => well.FacilityLinks.Any(link => link.Facility.FacilityId == filter.FacilityId));
        }

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            wells = wells.Where(well => well.Name.Contains(filter.Search) || well.Uwi.Contains(filter.Search));
        }

        return wells
            .OrderBy(well => well.WellId)
            .Select(well => new WellSummaryDto(
                well.WellId,
                well.Uwi,
                well.Name,
                well.LicenseeBaId,
                well.Field,
                well.StatusFluid,
                well.StatusMode,
                well.IsConfidential))
            .ToPagedResultAsync(page, cancellationToken);
    }

    public async Task<WellDetailDto?> GetAsync(string wellId, CancellationToken cancellationToken)
    {
        var well = await context.Wells
            .AsNoTracking()
            .Where(candidate => candidate.WellId == wellId)
            .Select(candidate => new
            {
                candidate.WellId,
                candidate.Uwi,
                candidate.Name,
                candidate.LicenceNumber,
                candidate.LicenseeBaId,
                candidate.Field,
                candidate.StatusFluid,
                candidate.StatusMode,
                candidate.LicenceStatus,
                candidate.IsConfidential,
                Facilities = candidate.FacilityLinks
                    .OrderByDescending(link => link.StartDate)
                    .Select(link => new WellFacilityDto(link.Facility.FacilityId, link.Facility.Name, link.StartDate))
                    .ToList(),
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (well is null)
        {
            return null;
        }

        var production = await context.MonthlyWellProduction
            .AsNoTracking()
            .Where(row => row.WellId == wellId)
            .OrderBy(row => row.ProductionMonth)
            .Select(ProductionQueryService.ToDto)
            .ToListAsync(cancellationToken);

        return new WellDetailDto(
            well.WellId,
            well.Uwi,
            well.Name,
            well.LicenceNumber,
            well.LicenseeBaId,
            well.Field,
            well.StatusFluid,
            well.StatusMode,
            well.LicenceStatus,
            well.IsConfidential,
            well.Facilities,
            production);
    }
}

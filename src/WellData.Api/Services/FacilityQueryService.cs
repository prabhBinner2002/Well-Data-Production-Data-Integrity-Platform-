using Microsoft.EntityFrameworkCore;
using WellData.Api.Dtos;
using WellData.Infrastructure.Persistence;

namespace WellData.Api.Services;

public sealed class FacilityQueryService(WellDataDbContext context)
{
    public Task<PagedResult<FacilitySummaryDto>> ListAsync(FacilityFilter filter, PageQuery page, CancellationToken cancellationToken)
    {
        var facilities = context.Facilities.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(filter.OperatorBaId))
        {
            facilities = facilities.Where(facility => facility.Operator.BaId == filter.OperatorBaId);
        }

        if (!string.IsNullOrWhiteSpace(filter.FacilityType))
        {
            facilities = facilities.Where(facility => facility.FacilityType == filter.FacilityType);
        }

        return facilities
            .OrderBy(facility => facility.FacilityId)
            .Select(facility => new FacilitySummaryDto(
                facility.FacilityId,
                facility.FacilityType,
                facility.Name,
                facility.SubTypeDescription,
                facility.Location,
                facility.OperationalStatus,
                facility.Operator.BaId,
                facility.Operator.LegalName))
            .ToPagedResultAsync(page, cancellationToken);
    }

    public Task<FacilityDetailDto?> GetAsync(string facilityId, CancellationToken cancellationToken) =>
        context.Facilities
            .AsNoTracking()
            .Where(facility => facility.FacilityId == facilityId)
            .Select(facility => new FacilityDetailDto(
                facility.FacilityId,
                facility.FacilityType,
                facility.Name,
                facility.SubType,
                facility.SubTypeDescription,
                facility.Location,
                facility.OperationalStatus,
                facility.Operator.BaId,
                facility.Operator.LegalName,
                facility.WellLinks
                    .OrderBy(link => link.Well.WellId)
                    .Select(link => new LinkedWellDto(link.Well.WellId, link.Well.Uwi, link.Well.Name, link.StartDate))
                    .ToList()))
            .SingleOrDefaultAsync(cancellationToken);
}

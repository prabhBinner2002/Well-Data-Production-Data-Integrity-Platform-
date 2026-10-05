using Microsoft.EntityFrameworkCore;
using WellData.Api.Dtos;
using WellData.Core.Entities;
using WellData.Infrastructure.Persistence;

namespace WellData.Api.Services;

public sealed class CostCentreService(WellDataDbContext context)
{
    public Task<PagedResult<CostCentreMappingDto>> ListAsync(PageQuery page, CancellationToken cancellationToken) =>
        context.ErpCostCentreMappings
            .AsNoTracking()
            .OrderBy(mapping => mapping.WellId)
            .Select(mapping => new CostCentreMappingDto(mapping.WellId, mapping.CostCentre, mapping.ModifiedBy, mapping.ModifiedAt))
            .ToPagedResultAsync(page, cancellationToken);

    /// <summary>Creates the well's mapping or changes its cost centre.</summary>
    public async Task<CostCentreMappingDto> SetAsync(string wellId, string costCentre, CancellationToken cancellationToken)
    {
        var mapping = await context.ErpCostCentreMappings.SingleOrDefaultAsync(candidate => candidate.WellId == wellId, cancellationToken);

        if (mapping is null)
        {
            mapping = new ErpCostCentreMapping { WellId = wellId, CostCentre = costCentre };
            context.ErpCostCentreMappings.Add(mapping);
        }
        else
        {
            mapping.CostCentre = costCentre;
        }

        await context.SaveChangesAsync(cancellationToken);
        return new CostCentreMappingDto(mapping.WellId, mapping.CostCentre, mapping.ModifiedBy ?? mapping.CreatedBy, mapping.ModifiedAt ?? mapping.CreatedAt);
    }
}

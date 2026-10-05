using Microsoft.AspNetCore.Mvc;
using WellData.Api.Dtos;
using WellData.Api.Services;

namespace WellData.Api.Controllers;

[ApiController]
[Route("api/facilities")]
public sealed class FacilitiesController(FacilityQueryService facilities) : ControllerBase
{
    /// <summary>Lists facilities, optionally filtered by operator or facility type.</summary>
    [HttpGet]
    public Task<PagedResult<FacilitySummaryDto>> List(
        [FromQuery] FacilityFilter filter,
        [FromQuery] PageQuery paging,
        CancellationToken cancellationToken) =>
        facilities.ListAsync(filter, paging, cancellationToken);

    /// <summary>Returns one facility with the wells linked to it.</summary>
    [HttpGet("{facilityId}")]
    [ProducesResponseType<FacilityDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FacilityDetailDto>> Get(string facilityId, CancellationToken cancellationToken)
    {
        var facility = await facilities.GetAsync(facilityId, cancellationToken);
        return facility is null ? NotFound() : facility;
    }
}

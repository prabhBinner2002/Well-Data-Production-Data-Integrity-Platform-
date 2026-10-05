using Microsoft.AspNetCore.Mvc;
using WellData.Api.Dtos;
using WellData.Api.Services;

namespace WellData.Api.Controllers;

[ApiController]
[Route("api/wells")]
public sealed class WellsController(WellQueryService wells) : ControllerBase
{
    /// <summary>Lists wells, optionally filtered by licensee, linked facility, or a name/UWI search.</summary>
    [HttpGet]
    public Task<PagedResult<WellSummaryDto>> List(
        [FromQuery] WellFilter filter,
        [FromQuery] PageQuery paging,
        CancellationToken cancellationToken) =>
        wells.ListAsync(filter, paging, cancellationToken);

    /// <summary>Returns one well with its linked facilities and monthly production history.</summary>
    [HttpGet("{wellId}")]
    [ProducesResponseType<WellDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WellDetailDto>> Get(string wellId, CancellationToken cancellationToken)
    {
        var well = await wells.GetAsync(wellId, cancellationToken);
        return well is null ? NotFound() : well;
    }
}

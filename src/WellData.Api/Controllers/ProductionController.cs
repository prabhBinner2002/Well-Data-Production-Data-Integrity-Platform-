using Microsoft.AspNetCore.Mvc;
using WellData.Api.Dtos;
using WellData.Api.Services;

namespace WellData.Api.Controllers;

[ApiController]
[Route("api/production")]
public sealed class ProductionController(ProductionQueryService production) : ControllerBase
{
    /// <summary>Monthly production per well, filtered by well, operator, and month range.</summary>
    [HttpGet]
    public Task<PagedResult<MonthlyProductionDto>> List(
        [FromQuery] ProductionFilter filter,
        [FromQuery] PageQuery paging,
        CancellationToken cancellationToken) =>
        production.ListAsync(filter, paging, cancellationToken);
}

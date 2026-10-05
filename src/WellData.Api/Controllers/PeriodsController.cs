using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WellData.Api.Auth;
using WellData.Api.Dtos;
using WellData.Core;
using WellData.Infrastructure.Periods;

namespace WellData.Api.Controllers;

[ApiController]
[Route("api/periods")]
public sealed class PeriodsController(PeriodService periods) : ControllerBase
{
    /// <summary>Lists every month that has loaded data with its close state, newest first.</summary>
    [HttpGet]
    public async Task<List<PeriodDto>> List(CancellationToken cancellationToken)
    {
        var summaries = await periods.ListAsync(cancellationToken);
        return summaries
            .Select(period => new PeriodDto(period.ProductionMonth, period.Status, period.ClosedBy, period.ClosedAt))
            .ToList();
    }

    /// <summary>Approves and locks a month. Afterwards its data and issues can no longer change.</summary>
    [HttpPost("{month}/close")]
    [Authorize(Roles = Roles.DataSteward)]
    [ProducesResponseType<PeriodDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PeriodDto>> Close(ProductionMonth month, CancellationToken cancellationToken)
    {
        var result = await periods.CloseAsync(month, cancellationToken);

        return result.Outcome switch
        {
            ClosePeriodOutcome.Closed => new PeriodDto(month, result.Period!.Status, result.Period.ClosedBy, result.Period.ClosedAt),
            ClosePeriodOutcome.AlreadyClosed => Conflict("Period is already closed.", $"{month} was closed earlier and cannot be closed again."),
            ClosePeriodOutcome.NoData => Conflict("Nothing to close.", $"No data has been loaded for {month}."),
            _ => throw new InvalidOperationException($"Unhandled outcome {result.Outcome}."),
        };
    }

    private ObjectResult Conflict(string title, string detail) =>
        Problem(statusCode: StatusCodes.Status409Conflict, title: title, detail: detail);
}

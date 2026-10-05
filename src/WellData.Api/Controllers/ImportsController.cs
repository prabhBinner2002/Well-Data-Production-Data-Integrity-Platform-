using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WellData.Api.Auth;
using WellData.Api.Dtos;
using WellData.Api.Services;
using WellData.Infrastructure.Ingestion.Volumetric;
using WellData.Infrastructure.Petrinex;

namespace WellData.Api.Controllers;

[ApiController]
[Route("api/imports")]
public sealed class ImportsController(ImportService imports) : ControllerBase
{
    /// <summary>Lists import runs, newest first.</summary>
    [HttpGet]
    public Task<PagedResult<ImportRunDto>> List([FromQuery] PageQuery paging, CancellationToken cancellationToken) =>
        imports.ListAsync(paging, cancellationToken);

    /// <summary>Imports a production month now. Returns outcome Unchanged when the file was already loaded.</summary>
    [HttpPost]
    [Authorize(Roles = Roles.DataSteward)]
    [ProducesResponseType<ImportResultDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ImportResultDto>> Trigger(TriggerImportRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await imports.ImportAsync(request.Month, cancellationToken);

            return result.Outcome == ImportOutcome.PeriodClosed
                ? Problem(
                    statusCode: StatusCodes.Status409Conflict,
                    title: "Period is closed.",
                    detail: $"{request.Month} has been closed and can no longer be imported.")
                : result;
        }
        catch (PetrinexFileNotAvailableException)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Month not published.",
                detail: $"Petrinex has no volumetric file for {request.Month}.");
        }
    }
}

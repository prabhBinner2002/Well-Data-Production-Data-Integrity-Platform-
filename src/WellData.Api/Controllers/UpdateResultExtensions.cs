using Microsoft.AspNetCore.Mvc;
using WellData.Api.Services;

namespace WellData.Api.Controllers;

internal static class UpdateResultExtensions
{
    /// <summary>Maps a service update outcome to 200, 404, or 409 with problem details.</summary>
    public static ActionResult<T> ToActionResult<T>(this ControllerBase controller, UpdateResult<T> result)
        where T : class =>
        result.Outcome switch
        {
            UpdateOutcome.Updated => result.Value!,
            UpdateOutcome.NotFound => controller.NotFound(),
            UpdateOutcome.ConcurrencyConflict => controller.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "The record was changed by someone else.",
                detail: "Read it again and retry with the new RowVersion."),
            UpdateOutcome.InvalidState => controller.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "The change is not allowed in the record's current state.",
                detail: result.Detail),
            _ => throw new InvalidOperationException($"Unhandled outcome {result.Outcome}."),
        };
}

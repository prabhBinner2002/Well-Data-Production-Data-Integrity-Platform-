using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WellData.Api.Auth;
using WellData.Api.Dtos;
using WellData.Api.Services;

namespace WellData.Api.Controllers;

[ApiController]
[Route("api/issues")]
public sealed class IssuesController(IssueService issues) : ControllerBase
{
    /// <summary>Lists data-quality issues, filtered by rule, severity, status, month, or well.</summary>
    [HttpGet]
    public Task<PagedResult<IssueDto>> List(
        [FromQuery] IssueFilter filter,
        [FromQuery] PageQuery paging,
        CancellationToken cancellationToken) =>
        issues.ListAsync(filter, paging, cancellationToken);

    [HttpGet("{id:long}")]
    [ProducesResponseType<IssueDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IssueDto>> Get(long id, CancellationToken cancellationToken)
    {
        var issue = await issues.GetAsync(id, cancellationToken);
        return issue is null ? NotFound() : issue;
    }

    /// <summary>Marks an issue as seen. Fails with 409 if it is already resolved or was changed since it was read.</summary>
    [HttpPost("{id:long}/acknowledge")]
    [Authorize(Roles = Roles.DataSteward)]
    [ProducesResponseType<IssueDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<IssueDto>> Acknowledge(long id, AcknowledgeIssueRequest request, CancellationToken cancellationToken) =>
        this.ToActionResult(await issues.AcknowledgeAsync(id, request.RowVersion, cancellationToken));

    /// <summary>Closes an issue with a note explaining the outcome. Fails with 409 if it is already resolved or was changed since it was read.</summary>
    [HttpPost("{id:long}/resolve")]
    [Authorize(Roles = Roles.DataSteward)]
    [ProducesResponseType<IssueDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<IssueDto>> Resolve(long id, ResolveIssueRequest request, CancellationToken cancellationToken) =>
        this.ToActionResult(await issues.ResolveAsync(id, request.Note, request.RowVersion, cancellationToken));
}

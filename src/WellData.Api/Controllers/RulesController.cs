using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WellData.Api.Auth;
using WellData.Api.Dtos;
using WellData.Api.Services;

namespace WellData.Api.Controllers;

[ApiController]
[Route("api/rules")]
public sealed class RulesController(RuleService rules) : ControllerBase
{
    /// <summary>Lists the rule catalog.</summary>
    [HttpGet]
    public Task<List<RuleDto>> List(CancellationToken cancellationToken) => rules.ListAsync(cancellationToken);

    /// <summary>Enables or disables a rule. A disabled rule is skipped on the next load.</summary>
    [HttpPut("{code}/enabled")]
    [Authorize(Roles = Roles.DataSteward)]
    [ProducesResponseType<RuleDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RuleDto>> SetEnabled(string code, SetRuleEnabledRequest request, CancellationToken cancellationToken) =>
        this.ToActionResult(await rules.SetEnabledAsync(code, request.IsEnabled, request.RowVersion, cancellationToken));
}

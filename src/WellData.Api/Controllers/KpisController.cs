using Microsoft.AspNetCore.Mvc;
using WellData.Api.Dtos;
using WellData.Api.Services;

namespace WellData.Api.Controllers;

[ApiController]
[Route("api/kpis")]
public sealed class KpisController(KpiService kpis) : ControllerBase
{
    /// <summary>Share of each month's rows that passed validation.</summary>
    [HttpGet("acceptance-rate")]
    public Task<List<AcceptanceRateDto>> AcceptanceRate(CancellationToken cancellationToken) =>
        kpis.GetAcceptanceRatesAsync(cancellationToken);

    /// <summary>Issue counts by month and rule.</summary>
    [HttpGet("issues")]
    public Task<List<IssueCountDto>> Issues(CancellationToken cancellationToken) =>
        kpis.GetIssueCountsAsync(cancellationToken);

    /// <summary>Total well production per month.</summary>
    [HttpGet("production-totals")]
    public Task<List<ProductionTotalDto>> ProductionTotals(CancellationToken cancellationToken) =>
        kpis.GetProductionTotalsAsync(cancellationToken);
}

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WellData.Api.Auth;
using WellData.Api.Dtos;
using WellData.Api.Services;
using WellData.Core;
using WellData.Core.Entities;
using WellData.Infrastructure.Erp;
using WellData.Infrastructure.Persistence;

namespace WellData.Api.Controllers;

[ApiController]
[Route("api/erp")]
public sealed class ErpController(CostCentreService costCentres, ErpExportService exports, WellDataDbContext context) : ControllerBase
{
    /// <summary>Lists the well to cost centre mappings.</summary>
    [HttpGet("cost-centres")]
    public Task<PagedResult<CostCentreMappingDto>> ListCostCentres([FromQuery] PageQuery paging, CancellationToken cancellationToken) =>
        costCentres.ListAsync(paging, cancellationToken);

    /// <summary>Sets the cost centre a well's volumes are booked to.</summary>
    [HttpPut("cost-centres/{wellId}")]
    [Authorize(Roles = Roles.DataSteward)]
    [ProducesResponseType<CostCentreMappingDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<CostCentreMappingDto> SetCostCentre(
        [StringLength(20, MinimumLength = 5)] string wellId,
        SetCostCentreRequest request,
        CancellationToken cancellationToken) =>
        costCentres.SetAsync(wellId, request.CostCentre, cancellationToken);

    /// <summary>Lists past ERP exports, newest first.</summary>
    [HttpGet("exports")]
    public Task<PagedResult<ErpExportDto>> ListExports([FromQuery] PageQuery paging, CancellationToken cancellationToken) =>
        context.ErpExports
            .AsNoTracking()
            .OrderByDescending(export => export.ExportedAt)
            .Select(export => new ErpExportDto(
                export.Id,
                export.ProductionMonth,
                export.FileName,
                export.RowCount,
                export.OilVolume,
                export.GasVolume,
                export.WaterVolume,
                export.CondensateVolume,
                export.UnmappedWellCount,
                export.ExportedAt,
                export.ExportedBy))
            .ToPagedResultAsync(paging, cancellationToken);

    /// <summary>Writes a closed month to an ERP import file. Fails with 409 while the month is still open.</summary>
    [HttpPost("exports/{month}")]
    [Authorize(Roles = Roles.DataSteward)]
    [ProducesResponseType<ErpExportDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ErpExportDto>> Export(ProductionMonth month, CancellationToken cancellationToken)
    {
        var result = await exports.ExportAsync(month, cancellationToken);

        return result.Outcome switch
        {
            ErpExportOutcome.Exported => ToDto(result.Export!),
            ErpExportOutcome.PeriodNotClosed => Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Period is not closed.",
                detail: $"{month} must be closed before it can be exported."),
            _ => throw new InvalidOperationException($"Unhandled outcome {result.Outcome}."),
        };
    }

    private static ErpExportDto ToDto(ErpExport export) =>
        new(
            export.Id,
            export.ProductionMonth,
            export.FileName,
            export.RowCount,
            export.OilVolume,
            export.GasVolume,
            export.WaterVolume,
            export.CondensateVolume,
            export.UnmappedWellCount,
            export.ExportedAt,
            export.ExportedBy);
}

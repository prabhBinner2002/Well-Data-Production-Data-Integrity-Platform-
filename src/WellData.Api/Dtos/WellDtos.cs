using System.ComponentModel.DataAnnotations;
using WellData.Core;

namespace WellData.Api.Dtos;

public sealed class WellFilter
{
    /// <summary>Licensee BA ID, for example 0XZ4.</summary>
    [StringLength(4)]
    public string? OperatorBaId { get; set; }

    /// <summary>Only wells linked to this facility, for example ABBT0131180.</summary>
    [StringLength(20)]
    public string? FacilityId { get; set; }

    /// <summary>Matches part of the well name or UWI.</summary>
    [StringLength(50)]
    public string? Search { get; set; }
}

public sealed record WellSummaryDto(
    string WellId,
    string Uwi,
    string Name,
    string? LicenseeBaId,
    string? Field,
    string? StatusFluid,
    string? StatusMode,
    bool IsConfidential);

public sealed record WellDetailDto(
    string WellId,
    string Uwi,
    string Name,
    string? LicenceNumber,
    string? LicenseeBaId,
    string? Field,
    string? StatusFluid,
    string? StatusMode,
    string? LicenceStatus,
    bool IsConfidential,
    IReadOnlyList<WellFacilityDto> Facilities,
    IReadOnlyList<MonthlyProductionDto> MonthlyProduction);

public sealed record WellFacilityDto(string FacilityId, string Name, DateOnly StartDate);

/// <summary>Liquids are in m3 and gas is in e3m3. A null volume means none was reported or it is masked.</summary>
public sealed record MonthlyProductionDto(
    ProductionMonth ProductionMonth,
    string WellId,
    string FacilityId,
    string OperatorBaId,
    decimal? OilVolume,
    decimal? GasVolume,
    decimal? WaterVolume,
    decimal? CondensateVolume,
    int? ProducingHours,
    bool IsMasked);

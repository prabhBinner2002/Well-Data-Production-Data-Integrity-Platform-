using System.ComponentModel.DataAnnotations;
using WellData.Core;

namespace WellData.Api.Dtos;

public sealed record CostCentreMappingDto(string WellId, string CostCentre, string? ModifiedBy, DateTime? ModifiedAt);

public sealed class SetCostCentreRequest
{
    /// <summary>ERP cost centre code: letters, digits, and dashes.</summary>
    [Required]
    [RegularExpression("^[A-Za-z0-9-]{1,20}$", ErrorMessage = "CostCentre must be 1 to 20 letters, digits, or dashes.")]
    public string CostCentre { get; set; } = string.Empty;
}

/// <summary>Volumes are totals over the exported file. Liquids are in m3 and gas is in e3m3.</summary>
public sealed record ErpExportDto(
    int Id,
    ProductionMonth ProductionMonth,
    string FileName,
    int RowCount,
    decimal OilVolume,
    decimal GasVolume,
    decimal WaterVolume,
    decimal CondensateVolume,
    int UnmappedWellCount,
    DateTime ExportedAt,
    string ExportedBy);

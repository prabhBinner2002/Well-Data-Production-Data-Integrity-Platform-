using WellData.Core;
using WellData.Core.Entities;

namespace WellData.Api.Dtos;

/// <param name="AcceptanceRate">Loaded rows divided by data rows, from 0 to 1. Proration header rows are not data rows.</param>
public sealed record AcceptanceRateDto(ProductionMonth ProductionMonth, int DataRows, int RowsLoaded, int RowsRejected, decimal AcceptanceRate);

public sealed record IssueCountDto(ProductionMonth ProductionMonth, string RuleCode, Severity Severity, int Count);

/// <summary>Liquids are in m3 and gas is in e3m3.</summary>
public sealed record ProductionTotalDto(
    ProductionMonth ProductionMonth,
    int WellCount,
    decimal OilVolume,
    decimal GasVolume,
    decimal WaterVolume,
    decimal CondensateVolume);

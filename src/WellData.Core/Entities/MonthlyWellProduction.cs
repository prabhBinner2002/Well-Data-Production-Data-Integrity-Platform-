namespace WellData.Core.Entities;

/// <summary>A row of vw_MonthlyWellProduction: one well's production for one month. Read-only.</summary>
public class MonthlyWellProduction
{
    public ProductionMonth ProductionMonth { get; set; }

    public required string WellId { get; set; }

    public required string FacilityId { get; set; }

    public required string OperatorBaId { get; set; }

    /// <summary>m3.</summary>
    public decimal? OilVolume { get; set; }

    /// <summary>e3m3.</summary>
    public decimal? GasVolume { get; set; }

    /// <summary>m3.</summary>
    public decimal? WaterVolume { get; set; }

    /// <summary>m3.</summary>
    public decimal? CondensateVolume { get; set; }

    public int? ProducingHours { get; set; }

    public bool IsMasked { get; set; }
}

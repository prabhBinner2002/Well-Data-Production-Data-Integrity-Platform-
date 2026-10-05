namespace WellData.Infrastructure.Validation;

/// <summary>Thresholds passed to the SQL rules as parameters.</summary>
public sealed class ValidationOptions
{
    public const string SectionName = "Validation";

    /// <summary>SPK-01: month-over-month change, in percent, above which a well is reported.</summary>
    public decimal SpikeThresholdPercent { get; set; } = 300;

    /// <summary>SPK-01 and RAT-01: volumes below this are too small to judge.</summary>
    public decimal MinimumVolume { get; set; } = 5;

    /// <summary>OUT-01: how many standard deviations from the trailing average counts as an outlier.</summary>
    public decimal OutlierStandardDeviations { get; set; } = 3;

    /// <summary>OUT-01: months of history a well needs before it is judged.</summary>
    public int MinimumHistoryMonths { get; set; } = 6;

    /// <summary>RAT-01: highest acceptable water share of total liquids, from 0 to 1.</summary>
    public decimal MaxWaterCut { get; set; } = 0.99m;

    /// <summary>RAT-01: highest acceptable gas-oil ratio in m3 of gas per m3 of oil.</summary>
    public decimal MaxGasOilRatio { get; set; } = 20_000;
}

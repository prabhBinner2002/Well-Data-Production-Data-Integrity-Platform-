namespace WellData.Core.Entities;

/// <summary>A Petrinex activity (PROD, DISP, INJ, ...). The code is the primary key.</summary>
public class ActivityCode
{
    public required string Code { get; set; }

    public required string Description { get; set; }

    /// <summary>True for adjustments and differences, where a negative volume is legitimate.</summary>
    public bool CanBeNegative { get; set; }

    /// <summary>False for status-only activities such as SHUTIN, which carry no product or volume.</summary>
    public bool ReportsVolume { get; set; } = true;
}

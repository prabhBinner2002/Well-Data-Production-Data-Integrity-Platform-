using WellData.Core.Entities;

namespace WellData.Core.Validation;

/// <summary>Rule codes and the default catalog rows seeded into the DataQualityRule table.</summary>
public static class RuleCatalog
{
    public const string Format = "FMT-01";
    public const string MissingVolume = "MIS-01";
    public const string MaskedValue = "MSK-01";
    public const string NegativeVolume = "RNG-01";
    public const string HoursExceedMonth = "RNG-02";
    public const string VolumeHoursConsistency = "CON-01";
    public const string UnknownCode = "REF-02";

    public const string Duplicate = "DUP-01";
    public const string UnknownWell = "REF-01";
    public const string ReportingGap = "GAP-01";
    public const string Spike = "SPK-01";
    public const string Outlier = "OUT-01";
    public const string Ratio = "RAT-01";
    public const string Restated = "RST-01";
    public const string UnmappedWell = "ERP-01";

    public static readonly DataQualityRule[] FileRules =
    [
        Rule(Format, "Unparseable number or month", Severity.Error),
        Rule(MissingVolume, "Blank volume on an activity that reports volume", Severity.Error),
        Rule(MaskedValue, "Value masked (***) for confidentiality", Severity.Info),
        Rule(NegativeVolume, "Negative volume on an activity that cannot be negative", Severity.Error),
        Rule(HoursExceedMonth, "Well hours greater than the hours in the month", Severity.Error),
        Rule(VolumeHoursConsistency, "Volume with 0 hours, or hours with no volume", Severity.Warning),
        Rule(UnknownCode, "Unknown product or activity code", Severity.Error),
    ];

    /// <summary>Set-based rules that run as SQL over loaded data. Each has a script in sql/rules named after its code.</summary>
    public static readonly DataQualityRule[] SqlRules =
    [
        Rule(Duplicate, "Duplicate row for the same facility, activity, product, and From/To ID", Severity.Error),
        Rule(UnknownWell, "From/To well is not in master data", Severity.Warning),
        Rule(ReportingGap, "Well reappears after months with nothing reported", Severity.Warning),
        Rule(Spike, "Month-over-month production change above the threshold", Severity.Warning),
        Rule(Outlier, "Production far from the well's trailing 12-month average", Severity.Warning),
        Rule(Ratio, "Water cut or gas-oil ratio outside bounds", Severity.Warning),
        Rule(Restated, "Value differs from the first loaded version of the month", Severity.Info),
        Rule(UnmappedWell, "Producing well has no ERP cost centre", Severity.Warning),
    ];

    private static DataQualityRule Rule(string code, string description, Severity severity) =>
        new() { Code = code, Description = description, Severity = severity, IsEnabled = true };
}

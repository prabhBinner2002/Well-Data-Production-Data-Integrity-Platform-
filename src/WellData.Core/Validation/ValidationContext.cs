using WellData.Core.Entities;

namespace WellData.Core.Validation;

/// <summary>Reference data the rules need: the month the file claims to be, and the known codes.</summary>
public sealed class ValidationContext(
    ProductionMonth fileMonth,
    IReadOnlyDictionary<string, ActivityCode> activities,
    IReadOnlySet<string> productCodes)
{
    public ProductionMonth FileMonth { get; } = fileMonth;

    public IReadOnlyDictionary<string, ActivityCode> Activities { get; } = activities;

    public IReadOnlySet<string> ProductCodes { get; } = productCodes;

    /// <summary>Unknown activities are assumed to report volume so that a blank volume on them is still caught.</summary>
    public bool ReportsVolume(string activityId) =>
        !Activities.TryGetValue(activityId, out var activity) || activity.ReportsVolume;
}

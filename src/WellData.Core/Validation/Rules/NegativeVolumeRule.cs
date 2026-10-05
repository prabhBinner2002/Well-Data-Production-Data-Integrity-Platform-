using WellData.Core.Volumetric;

namespace WellData.Core.Validation.Rules;

/// <summary>RNG-01: a negative volume where the activity (for example PROD) can never be negative.</summary>
public sealed class NegativeVolumeRule : RowRule
{
    public override string Code => RuleCatalog.NegativeVolume;

    protected override IEnumerable<RuleViolation> EvaluateRow(ParsedVolumetricRow row, ValidationContext context)
    {
        // Unknown activities are left to REF-02, since there is no way to know whether they may be negative.
        var isNegative = row.Volume is { IsValid: true, Value: < 0 };
        if (isNegative && context.Activities.TryGetValue(row.ActivityId, out var activity) && !activity.CanBeNegative)
        {
            yield return new RuleViolation(row, "Volume", row.Volume.Raw, $"Volume is negative, which {row.ActivityId} does not allow.");
        }
    }
}

using WellData.Core.Volumetric;

namespace WellData.Core.Validation.Rules;

/// <summary>MIS-01: no volume on an activity that should have one. SHUTIN rows legitimately have none.</summary>
public sealed class MissingVolumeRule : RowRule
{
    public override string Code => RuleCatalog.MissingVolume;

    protected override IEnumerable<RuleViolation> EvaluateRow(ParsedVolumetricRow row, ValidationContext context)
    {
        if (row.Volume.State == FieldState.Blank && context.ReportsVolume(row.ActivityId))
        {
            yield return new RuleViolation(row, "Volume", row.Volume.Raw, $"Volume is blank for activity {row.ActivityId}.");
        }
    }
}

using WellData.Core.Volumetric;

namespace WellData.Core.Validation.Rules;

/// <summary>CON-01: a well produced or injected volume in 0 hours, or was on for hours and moved nothing.</summary>
public sealed class VolumeHoursConsistencyRule : WellMonthRule
{
    private static readonly HashSet<string> FlowActivities = ["PROD", "INJ"];

    public override string Code => RuleCatalog.VolumeHoursConsistency;

    protected override IEnumerable<RuleViolation> EvaluateWell(IReadOnlyList<ParsedVolumetricRow> wellRows, ValidationContext context)
    {
        var flowRows = wellRows.Where(row => FlowActivities.Contains(row.ActivityId)).ToList();
        var hoursRow = RowWithMaxHours(wellRows);

        // A masked value means the real figure exists but is hidden, so no conclusion can be drawn.
        var anythingMasked = wellRows.Any(row => row.Hours.State == FieldState.Masked) ||
                             flowRows.Any(row => row.Volume.State == FieldState.Masked);

        if (flowRows.Count == 0 || hoursRow is null || anythingMasked)
        {
            yield break;
        }

        var hours = hoursRow.Hours.Value!.Value;
        var totalVolume = flowRows.Sum(row => row.Volume.Value ?? 0);

        if (hours == 0 && totalVolume > 0)
        {
            yield return new RuleViolation(hoursRow, "Hours", hoursRow.Hours.Raw, $"Well has a volume of {totalVolume} with 0 hours.");
        }
        else if (hours > 0 && totalVolume == 0)
        {
            yield return new RuleViolation(hoursRow, "Volume", hoursRow.Volume.Raw, $"Well reports {hours} hours with no volume.");
        }
    }
}

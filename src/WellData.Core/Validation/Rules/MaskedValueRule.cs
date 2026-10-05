using WellData.Core.Volumetric;

namespace WellData.Core.Validation.Rules;

/// <summary>MSK-01: records that Petrinex masked a value. Masked is a policy outcome, not missing data.</summary>
public sealed class MaskedValueRule : RowRule
{
    public override string Code => RuleCatalog.MaskedValue;

    protected override IEnumerable<RuleViolation> EvaluateRow(ParsedVolumetricRow row, ValidationContext context)
    {
        if (row.Hours.State == FieldState.Masked)
        {
            yield return Masked(row, "Hours");
        }

        if (row.Volume.State == FieldState.Masked)
        {
            yield return Masked(row, "Volume");
        }

        if (row.IsFromToMasked)
        {
            yield return Masked(row, "FromToID");
        }
    }

    private static RuleViolation Masked(ParsedVolumetricRow row, string field) =>
        new(row, field, ParsedField.Mask, $"{field} is masked for confidentiality.");
}

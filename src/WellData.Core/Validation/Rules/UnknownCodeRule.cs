using WellData.Core.Volumetric;

namespace WellData.Core.Validation.Rules;

/// <summary>REF-02: an activity or product code that is not in the reference tables.</summary>
public sealed class UnknownCodeRule : RowRule
{
    public override string Code => RuleCatalog.UnknownCode;

    protected override IEnumerable<RuleViolation> EvaluateRow(ParsedVolumetricRow row, ValidationContext context)
    {
        if (!context.Activities.ContainsKey(row.ActivityId))
        {
            yield return new RuleViolation(row, "ActivityID", row.ActivityId, $"Unknown activity code '{row.ActivityId}'.");
        }

        if (row.ProductId.Length == 0)
        {
            if (context.ReportsVolume(row.ActivityId))
            {
                yield return new RuleViolation(row, "ProductID", row.ProductId, $"Activity {row.ActivityId} requires a product code.");
            }
        }
        else if (!context.ProductCodes.Contains(row.ProductId))
        {
            yield return new RuleViolation(row, "ProductID", row.ProductId, $"Unknown product code '{row.ProductId}'.");
        }
    }
}

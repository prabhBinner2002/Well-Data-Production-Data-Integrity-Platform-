using WellData.Core.Volumetric;

namespace WellData.Core.Validation.Rules;

/// <summary>FMT-01: a month or number that cannot be read, a month that is not the file's month, or a corrupt ID.</summary>
public sealed class FormatRule : RowRule
{
    public override string Code => RuleCatalog.Format;

    protected override IEnumerable<RuleViolation> EvaluateRow(ParsedVolumetricRow row, ValidationContext context)
    {
        if (!row.Month.IsValid)
        {
            yield return new RuleViolation(row, "ProductionMonth", row.Month.Raw, "Production month is missing or not in YYYY-MM format.");
        }
        else if (row.Month.Value != context.FileMonth)
        {
            yield return new RuleViolation(row, "ProductionMonth", row.Month.Raw, $"Production month does not match the file's month {context.FileMonth}.");
        }

        if (row.Volume.State == FieldState.Invalid)
        {
            yield return new RuleViolation(row, "Volume", row.Volume.Raw, "Volume is not a number.");
        }

        if (row.Energy.State == FieldState.Invalid)
        {
            yield return new RuleViolation(row, "Energy", row.Energy.Raw, "Energy is not a number.");
        }

        if (row.Hours.State == FieldState.Invalid)
        {
            yield return new RuleViolation(row, "Hours", row.Hours.Raw, "Hours is not a whole number.");
        }

        if (row.HasOversizedIdentifier)
        {
            yield return new RuleViolation(row, "FromToID", row.FromToId, "An identifier on this row is longer than any Petrinex ID.");
        }
    }
}

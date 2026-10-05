using WellData.Core.Volumetric;

namespace WellData.Core.Validation;

/// <summary>Base class for rules that judge each row on its own.</summary>
public abstract class RowRule : IValidationRule
{
    public abstract string Code { get; }

    public IEnumerable<RuleViolation> Evaluate(IReadOnlyList<ParsedVolumetricRow> rows, ValidationContext context) =>
        rows.SelectMany(row => EvaluateRow(row, context));

    protected abstract IEnumerable<RuleViolation> EvaluateRow(ParsedVolumetricRow row, ValidationContext context);
}

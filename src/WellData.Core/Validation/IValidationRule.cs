using WellData.Core.Volumetric;

namespace WellData.Core.Validation;

/// <summary>
/// A data-quality check over a file's volumetric rows (Strategy pattern).
/// Rules see the whole batch because some checks, such as hours, are per well rather than per row.
/// </summary>
public interface IValidationRule
{
    string Code { get; }

    IEnumerable<RuleViolation> Evaluate(IReadOnlyList<ParsedVolumetricRow> rows, ValidationContext context);
}

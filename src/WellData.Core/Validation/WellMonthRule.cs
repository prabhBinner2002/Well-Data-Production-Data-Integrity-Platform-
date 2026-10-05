using WellData.Core.Volumetric;

namespace WellData.Core.Validation;

/// <summary>
/// Base class for rules about a well's month as a whole.
/// Petrinex usually reports a well's hours on only one of its product rows, so hours can only be
/// judged across all of the well's rows at a facility, never row by row.
/// </summary>
public abstract class WellMonthRule : IValidationRule
{
    public abstract string Code { get; }

    public IEnumerable<RuleViolation> Evaluate(IReadOnlyList<ParsedVolumetricRow> rows, ValidationContext context) =>
        rows
            .Where(row => row.WellId is not null)
            .GroupBy(row => (row.ReportingFacilityId, row.WellId))
            .SelectMany(wellRows => EvaluateWell(wellRows.ToList(), context));

    protected abstract IEnumerable<RuleViolation> EvaluateWell(IReadOnlyList<ParsedVolumetricRow> wellRows, ValidationContext context);

    /// <summary>The row carrying the well's highest reported hours, or null when no row has readable hours.</summary>
    protected static ParsedVolumetricRow? RowWithMaxHours(IReadOnlyList<ParsedVolumetricRow> wellRows) =>
        wellRows
            .Where(row => row.Hours.IsValid)
            .MaxBy(row => row.Hours.Value);
}

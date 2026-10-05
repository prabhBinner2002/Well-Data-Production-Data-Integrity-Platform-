using WellData.Core.Entities;
using WellData.Core.Volumetric;

namespace WellData.Core.Validation;

/// <summary>A violation together with the catalog rule that raised it.</summary>
public sealed record RuleFinding(DataQualityRule Rule, RuleViolation Violation);

public sealed class ValidationResult(IReadOnlyList<RuleFinding> findings)
{
    private readonly HashSet<ParsedVolumetricRow> _rejected = RowsWith(findings, Severity.Error);
    private readonly HashSet<ParsedVolumetricRow> _warned = RowsWith(findings, Severity.Warning);

    public IReadOnlyList<RuleFinding> Findings { get; } = findings;

    /// <summary>True when the row has at least one Error finding and must be quarantined.</summary>
    public bool IsRejected(ParsedVolumetricRow row) => _rejected.Contains(row);

    public bool HasWarning(ParsedVolumetricRow row) => _warned.Contains(row);

    private static HashSet<ParsedVolumetricRow> RowsWith(IEnumerable<RuleFinding> findings, Severity severity) =>
        findings
            .Where(finding => finding.Rule.Severity == severity)
            .Select(finding => finding.Violation.Row)
            .ToHashSet();
}

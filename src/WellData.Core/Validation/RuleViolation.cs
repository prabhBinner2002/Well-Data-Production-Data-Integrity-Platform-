using WellData.Core.Volumetric;

namespace WellData.Core.Validation;

/// <summary>What a rule found wrong with a row. Severity comes from the rule catalog, not the rule itself.</summary>
public sealed record RuleViolation(ParsedVolumetricRow Row, string Field, string? RawValue, string Message);

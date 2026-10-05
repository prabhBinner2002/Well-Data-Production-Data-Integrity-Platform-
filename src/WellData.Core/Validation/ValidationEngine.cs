using WellData.Core.Entities;
using WellData.Core.Validation.Rules;
using WellData.Core.Volumetric;

namespace WellData.Core.Validation;

/// <summary>Runs every enabled rule over a file's rows. Adding a rule means adding a class, not editing this one.</summary>
public sealed class ValidationEngine(IEnumerable<IValidationRule> rules)
{
    private readonly IReadOnlyList<IValidationRule> _rules = rules.ToList();

    /// <summary>An engine with all the built-in file rules.</summary>
    public static ValidationEngine CreateDefault() => new(
    [
        new FormatRule(),
        new MissingVolumeRule(),
        new MaskedValueRule(),
        new NegativeVolumeRule(),
        new HoursExceedMonthRule(),
        new VolumeHoursConsistencyRule(),
        new UnknownCodeRule(),
    ]);

    /// <param name="rows">Volumetric rows only. Proration header rows must already be filtered out.</param>
    /// <param name="catalog">Catalog rows by code. A rule that is disabled or missing from the catalog does not run.</param>
    public ValidationResult Validate(
        IReadOnlyList<ParsedVolumetricRow> rows,
        ValidationContext context,
        IReadOnlyDictionary<string, DataQualityRule> catalog)
    {
        var findings = new List<RuleFinding>();

        foreach (var rule in _rules)
        {
            if (!catalog.TryGetValue(rule.Code, out var catalogRule) || !catalogRule.IsEnabled)
            {
                continue;
            }

            findings.AddRange(rule.Evaluate(rows, context).Select(violation => new RuleFinding(catalogRule, violation)));
        }

        return new ValidationResult(findings);
    }
}

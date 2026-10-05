using WellData.Core.Volumetric;

namespace WellData.Core.Validation.Rules;

/// <summary>RNG-02: a well reports more hours than the month contains.</summary>
public sealed class HoursExceedMonthRule : WellMonthRule
{
    public override string Code => RuleCatalog.HoursExceedMonth;

    protected override IEnumerable<RuleViolation> EvaluateWell(IReadOnlyList<ParsedVolumetricRow> wellRows, ValidationContext context)
    {
        var hoursInMonth = context.FileMonth.HoursInMonth;
        var busiest = RowWithMaxHours(wellRows);

        if (busiest is not null && busiest.Hours.Value > hoursInMonth)
        {
            yield return new RuleViolation(
                busiest,
                "Hours",
                busiest.Hours.Raw,
                $"Well reports {busiest.Hours.Value} hours, but {context.FileMonth} has only {hoursInMonth}.");
        }
    }
}

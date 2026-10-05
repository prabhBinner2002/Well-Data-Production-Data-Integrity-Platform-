using WellData.Core.Entities;
using WellData.Core.Validation;

namespace WellData.Tests.Unit.Validation;

public class ValidationEngineTests
{
    private readonly ValidationEngine _engine = ValidationEngine.CreateDefault();

    [Fact]
    public void Row_with_an_error_is_rejected_and_a_clean_row_is_not()
    {
        var bad = TestRows.Row(volume: "-5");
        var good = TestRows.Row(fromTo: TestRows.OtherWell);

        var result = _engine.Validate([bad, good], TestRows.Context(), TestRows.Catalog());

        Assert.True(result.IsRejected(bad));
        Assert.False(result.IsRejected(good));
        Assert.Equal(RuleCatalog.NegativeVolume, Assert.Single(result.Findings).Rule.Code);
    }

    [Fact]
    public void Row_with_only_a_warning_is_flagged_but_not_rejected()
    {
        var row = TestRows.Row(volume: "49.9", hours: "0");

        var result = _engine.Validate([row], TestRows.Context(), TestRows.Catalog());

        Assert.False(result.IsRejected(row));
        Assert.True(result.HasWarning(row));
    }

    [Fact]
    public void Info_findings_neither_reject_nor_flag_the_row()
    {
        var row = TestRows.Row(hours: "***");

        var result = _engine.Validate([row], TestRows.Context(), TestRows.Catalog());

        Assert.Equal(Severity.Info, Assert.Single(result.Findings).Rule.Severity);
        Assert.False(result.IsRejected(row));
        Assert.False(result.HasWarning(row));
    }

    [Fact]
    public void Disabled_rule_does_not_run()
    {
        var catalog = TestRows.Catalog();
        catalog[RuleCatalog.NegativeVolume].IsEnabled = false;
        var row = TestRows.Row(volume: "-5");

        var result = _engine.Validate([row], TestRows.Context(), catalog);

        Assert.Empty(result.Findings);
        Assert.False(result.IsRejected(row));
    }

    [Fact]
    public void Severity_comes_from_the_catalog_so_a_downgraded_rule_no_longer_rejects()
    {
        var catalog = TestRows.Catalog();
        catalog[RuleCatalog.NegativeVolume].Severity = Severity.Warning;
        var row = TestRows.Row(volume: "-5");

        var result = _engine.Validate([row], TestRows.Context(), catalog);

        Assert.False(result.IsRejected(row));
        Assert.True(result.HasWarning(row));
    }

    [Fact]
    public void A_row_can_collect_findings_from_several_rules()
    {
        var row = TestRows.Row(activity: "VNTX", product: "C9-ZZ", volume: "abc");

        var result = _engine.Validate([row], TestRows.Context(), TestRows.Catalog());

        Assert.Equal(
            [RuleCatalog.Format, RuleCatalog.UnknownCode, RuleCatalog.UnknownCode],
            result.Findings.Select(finding => finding.Rule.Code).Order());
    }

    [Fact]
    public async Task Clean_real_month_raises_only_masked_value_infos()
    {
        var rows = await TestRows.FixtureDataRowsAsync();

        var result = _engine.Validate(rows, TestRows.Context(), TestRows.Catalog());

        Assert.Equal(19, result.Findings.Count);
        Assert.All(result.Findings, finding => Assert.Equal(RuleCatalog.MaskedValue, finding.Rule.Code));
        Assert.DoesNotContain(rows, result.IsRejected);
    }

    [Fact]
    public async Task Error_injected_month_raises_exactly_the_injected_issues()
    {
        var rows = await TestRows.FixtureDataRowsAsync(FixtureFiles.VolumetricWithErrors);

        var result = _engine.Validate(rows, TestRows.Context(), TestRows.Catalog());

        var countsByRule = result.Findings
            .GroupBy(finding => finding.Rule.Code)
            .ToDictionary(group => group.Key, group => group.Count());

        Assert.Equal(2, countsByRule[RuleCatalog.Format]);
        Assert.Equal(1, countsByRule[RuleCatalog.MissingVolume]);
        Assert.Equal(1, countsByRule[RuleCatalog.NegativeVolume]);
        Assert.Equal(1, countsByRule[RuleCatalog.HoursExceedMonth]);
        Assert.Equal(1, countsByRule[RuleCatalog.VolumeHoursConsistency]);
        Assert.Equal(2, countsByRule[RuleCatalog.UnknownCode]);
        Assert.Equal(19, countsByRule[RuleCatalog.MaskedValue]);
        Assert.Equal(7, rows.Count(result.IsRejected));
        Assert.Equal(1, rows.Count(result.HasWarning));
    }
}

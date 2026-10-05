using WellData.Core.Validation.Rules;

namespace WellData.Tests.Unit.Validation;

public class UnknownCodeRuleTests
{
    private readonly UnknownCodeRule _rule = new();

    [Fact]
    public void Known_activity_and_product_pass()
    {
        Assert.Empty(_rule.Evaluate([TestRows.Row(activity: "PROD", product: "OIL")], TestRows.Context()));
    }

    [Fact]
    public void Unknown_activity_code_is_an_error()
    {
        var violation = Assert.Single(_rule.Evaluate([TestRows.Row(activity: "VNTX", product: "GAS")], TestRows.Context()));

        Assert.Equal("ActivityID", violation.Field);
        Assert.Equal("VNTX", violation.RawValue);
    }

    [Fact]
    public void Unknown_product_code_is_an_error()
    {
        var violation = Assert.Single(_rule.Evaluate([TestRows.Row(activity: "REC", product: "C9-ZZ")], TestRows.Context()));

        Assert.Equal("ProductID", violation.Field);
        Assert.Equal("C9-ZZ", violation.RawValue);
    }

    [Fact]
    public void Codes_are_case_sensitive_like_the_reference_tables()
    {
        Assert.Single(_rule.Evaluate([TestRows.Row(activity: "prod")], TestRows.Context()));
    }

    [Fact]
    public void Missing_product_on_a_volume_activity_is_an_error()
    {
        var violation = Assert.Single(_rule.Evaluate([TestRows.Row(activity: "PROD", product: "")], TestRows.Context()));

        Assert.Equal("ProductID", violation.Field);
    }

    [Fact]
    public void Missing_product_on_SHUTIN_is_expected()
    {
        Assert.Empty(_rule.Evaluate([TestRows.Row(activity: "SHUTIN", product: "", volume: null)], TestRows.Context()));
    }

    [Fact]
    public async Task Real_fixture_month_uses_only_known_codes()
    {
        Assert.Empty(_rule.Evaluate(await TestRows.FixtureDataRowsAsync(), TestRows.Context()));
    }
}

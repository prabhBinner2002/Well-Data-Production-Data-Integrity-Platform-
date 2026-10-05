using WellData.Core.Validation;
using WellData.Tests.Integration.Infrastructure;

namespace WellData.Tests.Integration.Validation;

public class UnknownWellRuleTests(SqlServerFixture sqlServer) : SqlRuleTest(sqlServer)
{
    [Fact]
    public async Task Well_missing_from_master_data_is_reported_once_however_many_rows_it_has()
    {
        await Harness.IngestMasterAsync();
        await Seed
            .Production(June, "ABWI100999999999W400", oil: 10, gas: 2, water: 1)
            .Production(June, VolumetricSeeder.WellA, oil: 38.6m)
            .SaveAsync();

        var issue = Assert.Single(await RunAndGetIssuesAsync(RuleCatalog.UnknownWell));

        Assert.Equal("ABWI100999999999W400", issue.WellId);
        Assert.Equal("FromToID", issue.Field);
        Assert.Contains("3 rows", issue.Message);
    }

    [Fact]
    public async Task Facility_and_masked_from_to_ids_are_not_checked_against_wells()
    {
        await Harness.IngestMasterAsync();
        await Seed
            .Record(June, "DISP", "OIL", 180.9m, fromToId: "ABBT0040518", fromToIdType: "BT")
            .Record(June, "DISP", "WATER", 8.6m, fromToId: "***", isMasked: true)
            .SaveAsync();

        Assert.Empty(await RunAndGetIssuesAsync(RuleCatalog.UnknownWell));
    }

    [Fact]
    public async Task Real_month_reports_the_five_wells_whose_battery_changed_operator()
    {
        await Harness.IngestMasterAsync();
        await Harness.IngestFixtureMonthAsync();

        var issues = await GetIssuesAsync(RuleCatalog.UnknownWell);

        Assert.Equal(
            [
                "ABWI100062407209W600",
                "ABWI100052107409W602",
                "ABWI100082107409W600",
                "ABWI100132107409W600",
                "ABWI100162107409W600",
            ],
            issues.Select(issue => issue.WellId));
        Assert.Equal(["ABBT0058572", "ABBT0113245"], issues.Select(issue => issue.FacilityId).Distinct());
    }
}

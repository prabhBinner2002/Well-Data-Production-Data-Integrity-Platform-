using WellData.Core.Validation;
using WellData.Tests.Integration.Infrastructure;

namespace WellData.Tests.Integration.Validation;

public class DuplicateRuleTests(SqlServerFixture sqlServer) : SqlRuleTest(sqlServer)
{
    [Fact]
    public async Task Second_copy_of_a_row_is_reported_once()
    {
        await Seed
            .Record(June, "PROD", "OIL", 38.6m, VolumetricSeeder.WellA)
            .Record(June, "PROD", "OIL", 38.6m, VolumetricSeeder.WellA)
            .Record(June, "PROD", "GAS", 9.8m, VolumetricSeeder.WellA)
            .SaveAsync();

        var issue = Assert.Single(await RunAndGetIssuesAsync(RuleCatalog.Duplicate));

        Assert.Equal(VolumetricSeeder.Facility, issue.FacilityId);
        Assert.Equal(VolumetricSeeder.WellA, issue.WellId);
        Assert.Contains("PROD OIL", issue.Message);
        Assert.Equal(June, issue.ProductionMonth);
    }

    [Fact]
    public async Task Three_copies_report_the_two_extras()
    {
        await Seed
            .Record(June, "PROD", "OIL", 38.6m, VolumetricSeeder.WellA)
            .Record(June, "PROD", "OIL", 38.6m, VolumetricSeeder.WellA)
            .Record(June, "PROD", "OIL", 40.0m, VolumetricSeeder.WellA)
            .SaveAsync();

        Assert.Equal(2, (await RunAndGetIssuesAsync(RuleCatalog.Duplicate)).Count);
    }

    [Fact]
    public async Task Rows_without_a_from_to_id_are_compared_too()
    {
        await Seed
            .Record(June, "INVCL", "OIL", 2.8m)
            .Record(June, "INVCL", "OIL", 2.8m)
            .SaveAsync();

        var issue = Assert.Single(await RunAndGetIssuesAsync(RuleCatalog.Duplicate));

        Assert.Null(issue.WellId);
    }

    [Fact]
    public async Task Different_wells_products_facilities_or_months_are_not_duplicates()
    {
        await Seed
            .Record(June, "PROD", "OIL", 38.6m, VolumetricSeeder.WellA)
            .Record(June, "PROD", "OIL", 38.6m, VolumetricSeeder.WellB)
            .Record(June, "PROD", "GAS", 38.6m, VolumetricSeeder.WellA)
            .Record(June, "PROD", "OIL", 38.6m, VolumetricSeeder.WellA, facility: "ABBT0170649")
            .Record(June.AddMonths(-1), "PROD", "OIL", 38.6m, VolumetricSeeder.WellA)
            .SaveAsync();

        Assert.Empty(await RunAndGetIssuesAsync(RuleCatalog.Duplicate));
    }

    [Fact]
    public async Task Masked_rows_are_excluded_from_the_duplicate_check()
    {
        await Seed
            .Record(June, "DISP", "WATER", 8.6m, fromToId: "***", isMasked: true)
            .Record(June, "DISP", "WATER", 1.3m, fromToId: "***", isMasked: true)
            .SaveAsync();

        Assert.Empty(await RunAndGetIssuesAsync(RuleCatalog.Duplicate));
    }

    [Fact]
    public async Task Repeated_real_row_in_the_error_fixture_is_found_after_ingestion()
    {
        Harness.Petrinex.SetVolumetricFile(June, FixtureFiles.PathTo(FixtureFiles.VolumetricWithErrors));
        await Harness.IngestFixtureMonthAsync();

        var issue = Assert.Single(await GetIssuesAsync(RuleCatalog.Duplicate));

        Assert.Equal("ABBT0170649", issue.FacilityId);
        Assert.Equal("ABWI100040307406W600", issue.WellId);
        Assert.Equal("525.500", issue.RawValue);
    }
}

using WellData.Api.Dtos;
using WellData.Core.Entities;
using WellData.Core.Validation;
using WellData.Tests.Integration.Infrastructure;

namespace WellData.Tests.Integration.Api;

public class KpisEndpointTests(SqlServerFixture sqlServer) : ApiTest(sqlServer)
{
    [Fact]
    public async Task Acceptance_rate_is_loaded_rows_over_data_rows()
    {
        Harness.Petrinex.SetVolumetricFile(IngestionHarness.FixtureMonth, FixtureFiles.PathTo(FixtureFiles.VolumetricWithErrors));
        await Harness.IngestFixtureMonthAsync();

        var month = Assert.Single(await GetAsync<List<AcceptanceRateDto>>("/api/kpis/acceptance-rate"));

        Assert.Equal(IngestionHarness.FixtureMonth, month.ProductionMonth);
        Assert.Equal(125, month.DataRows);
        Assert.Equal(118, month.RowsLoaded);
        Assert.Equal(7, month.RowsRejected);
        Assert.Equal(0.944m, month.AcceptanceRate);
    }

    [Fact]
    public async Task Acceptance_rate_uses_the_file_currently_loaded_not_a_superseded_one()
    {
        Harness.Petrinex.SetVolumetricFile(IngestionHarness.FixtureMonth, FixtureFiles.PathTo(FixtureFiles.VolumetricWithErrors));
        await Harness.IngestFixtureMonthAsync();
        Harness.Petrinex.SetVolumetricFile(IngestionHarness.FixtureMonth, FixtureFiles.PathTo(FixtureFiles.Volumetric));
        await Harness.IngestFixtureMonthAsync();

        var month = Assert.Single(await GetAsync<List<AcceptanceRateDto>>("/api/kpis/acceptance-rate"));

        Assert.Equal(124, month.RowsLoaded);
        Assert.Equal(1m, month.AcceptanceRate);
    }

    [Fact]
    public async Task Issue_counts_are_grouped_by_month_and_rule()
    {
        await SeedFixtureDataAsync();

        var counts = await GetAsync<List<IssueCountDto>>("/api/kpis/issues");

        Assert.Equal(2, counts.Count);
        var masked = counts.Single(count => count.RuleCode == RuleCatalog.MaskedValue);
        Assert.Equal(19, masked.Count);
        Assert.Equal(Severity.Info, masked.Severity);
        Assert.Equal(5, counts.Single(count => count.RuleCode == RuleCatalog.UnknownWell).Count);
    }

    [Fact]
    public async Task Production_totals_sum_every_producing_well_in_the_month()
    {
        await SeedFixtureDataAsync();

        var month = Assert.Single(await GetAsync<List<ProductionTotalDto>>("/api/kpis/production-totals"));

        Assert.Equal(IngestionHarness.FixtureMonth, month.ProductionMonth);
        Assert.Equal(12, month.WellCount);
        Assert.Equal(2523.2m, month.OilVolume);
        Assert.Equal(0m, month.CondensateVolume);
    }

    [Fact]
    public async Task Kpis_are_empty_before_anything_is_loaded()
    {
        Assert.Empty(await GetAsync<List<AcceptanceRateDto>>("/api/kpis/acceptance-rate"));
        Assert.Empty(await GetAsync<List<IssueCountDto>>("/api/kpis/issues"));
        Assert.Empty(await GetAsync<List<ProductionTotalDto>>("/api/kpis/production-totals"));
    }
}

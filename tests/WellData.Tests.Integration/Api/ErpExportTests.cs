using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using WellData.Api.Auth;
using WellData.Api.Dtos;
using WellData.Core.Validation;
using WellData.Infrastructure.Erp;
using WellData.Tests.Integration.Infrastructure;

namespace WellData.Tests.Integration.Api;

public class ErpExportTests(SqlServerFixture sqlServer) : ApiTest(sqlServer)
{
    private const string MappedWell = "ABWI100013604905W500";

    [Fact]
    public async Task Cost_centre_can_be_set_listed_and_changed()
    {
        var created = await Client.PutAsJsonAsync($"/api/erp/cost-centres/{MappedWell}", new { costCentre = "CC-1001" });
        var changed = await Client.PutAsJsonAsync($"/api/erp/cost-centres/{MappedWell}", new { costCentre = "CC-2002" });

        Assert.Equal(HttpStatusCode.OK, created.StatusCode);
        Assert.Equal("CC-1001", (await ReadAsync<CostCentreMappingDto>(created)).CostCentre);
        Assert.Equal("CC-2002", (await ReadAsync<CostCentreMappingDto>(changed)).CostCentre);
        var mapping = Assert.Single((await GetAsync<PagedResult<CostCentreMappingDto>>("/api/erp/cost-centres")).Items);
        Assert.Equal(MappedWell, mapping.WellId);
        Assert.Equal("CC-2002", mapping.CostCentre);
        Assert.Equal(ApiFactory.StewardUserName, mapping.ModifiedBy);
    }

    [Theory]
    [InlineData("")]
    [InlineData("CC 1001")]
    [InlineData("CC-1001,EVIL")]
    [InlineData("ABCDEFGHIJKLMNOPQRSTU")]
    public async Task Cost_centre_that_would_break_the_export_file_is_rejected(string costCentre)
    {
        var response = await Client.PutAsJsonAsync($"/api/erp/cost-centres/{MappedWell}", new { costCentre });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        AssertProblemDetails(response);
    }

    [Fact]
    public async Task Viewer_cannot_set_a_cost_centre_or_export()
    {
        var viewer = await CreateClientAsync(Roles.Viewer);

        var mapping = await viewer.PutAsJsonAsync($"/api/erp/cost-centres/{MappedWell}", new { costCentre = "CC-1001" });
        var export = await viewer.PostAsync("/api/erp/exports/2026-06", content: null);

        Assert.Equal(HttpStatusCode.Forbidden, mapping.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, export.StatusCode);
    }

    [Fact]
    public async Task Unmapped_producing_wells_raise_ERP_01_once_mapping_has_started()
    {
        await SeedFixtureDataAsync();
        Assert.Equal(0, (await GetAsync<PagedResult<IssueDto>>($"/api/issues?ruleCode={RuleCatalog.UnmappedWell}")).TotalCount);

        await Client.PutAsJsonAsync($"/api/erp/cost-centres/{MappedWell}", new { costCentre = "CC-1001" });
        await Harness.RunSqlRulesAsync(IngestionHarness.FixtureMonth);

        // Twelve wells produced in June and one of them is now mapped.
        var issues = await GetAsync<PagedResult<IssueDto>>($"/api/issues?ruleCode={RuleCatalog.UnmappedWell}&pageSize=200");
        Assert.Equal(11, issues.TotalCount);
        Assert.DoesNotContain(issues.Items, issue => issue.WellId == MappedWell);
        Assert.All(issues.Items, issue => Assert.Equal("CostCentre", issue.Field));
    }

    [Fact]
    public async Task Open_month_cannot_be_exported()
    {
        await SeedFixtureDataAsync();

        var response = await Client.PostAsync("/api/erp/exports/2026-06", content: null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        AssertProblemDetails(response);
        Assert.False(Directory.Exists(Factory.ExportDirectory) && Directory.EnumerateFiles(Factory.ExportDirectory).Any());
    }

    [Fact]
    public async Task Export_totals_match_the_database_totals_for_the_month()
    {
        var export = await SeedCloseAndExportAsync();

        await using var context = Database.CreateContext();
        var databaseTotals = await context.VolumetricRecords
            .AsNoTracking()
            .Where(record => record.ProductionMonth == IngestionHarness.FixtureMonth)
            .Where(record => record.ActivityId == "PROD" && record.FromToIdType == "WI")
            .GroupBy(record => record.ProductId!)
            .Select(group => new { Product = group.Key, Volume = group.Sum(record => record.Volume ?? 0) })
            .ToDictionaryAsync(row => row.Product, row => row.Volume);

        Assert.Equal(databaseTotals["OIL"], export.OilVolume);
        Assert.Equal(databaseTotals["GAS"], export.GasVolume);
        Assert.Equal(databaseTotals["WATER"], export.WaterVolume);
        Assert.Equal(0m, export.CondensateVolume);
        Assert.Equal(2523.2m, export.OilVolume);
    }

    [Fact]
    public async Task Export_file_has_the_fixed_layout_and_adds_up_to_the_recorded_totals()
    {
        var export = await SeedCloseAndExportAsync();

        var lines = await File.ReadAllLinesAsync(Path.Combine(Factory.ExportDirectory, export.FileName));

        Assert.Equal(ErpExportService.Header, lines[0]);
        var rows = lines.Skip(1).Select(line => line.Split(',')).ToList();
        Assert.Equal(export.RowCount, rows.Count);
        Assert.Equal(35, rows.Count);
        Assert.All(rows, row =>
        {
            Assert.Equal(6, row.Length);
            Assert.Equal("2026-06", row[0]);
            Assert.Equal(row[3] == "GAS" ? "E3M3" : "M3", row[5]);
        });
        Assert.Equal(export.OilVolume, SumOf(rows, "OIL"));
        Assert.Equal(export.GasVolume, SumOf(rows, "GAS"));
        Assert.Equal(export.WaterVolume, SumOf(rows, "WATER"));
        Assert.Contains("2026-06,CC-1001,ABWI100013604905W500,OIL,38.600,M3", lines);
    }

    [Fact]
    public async Task Unmapped_wells_are_exported_under_a_placeholder_and_counted()
    {
        var export = await SeedCloseAndExportAsync();

        var lines = await File.ReadAllLinesAsync(Path.Combine(Factory.ExportDirectory, export.FileName));

        Assert.Equal(11, export.UnmappedWellCount);
        Assert.Equal(3, lines.Count(line => line.Contains(",CC-1001,")));
        Assert.Equal(32, lines.Count(line => line.Contains($",{ErpExportService.UnmappedCostCentre},")));
    }

    [Fact]
    public async Task Export_is_recorded_with_who_ran_it()
    {
        var export = await SeedCloseAndExportAsync();

        var listed = Assert.Single((await GetAsync<PagedResult<ErpExportDto>>("/api/erp/exports")).Items);

        Assert.Equal(export.Id, listed.Id);
        Assert.Equal(export.FileName, listed.FileName);
        Assert.Equal(ApiFactory.StewardUserName, listed.ExportedBy);
        Assert.Equal(IngestionHarness.FixtureMonth, listed.ProductionMonth);
        Assert.StartsWith("erp_production_2026-06_", listed.FileName);
    }

    private async Task<ErpExportDto> SeedCloseAndExportAsync()
    {
        await SeedFixtureDataAsync();
        await Client.PutAsJsonAsync($"/api/erp/cost-centres/{MappedWell}", new { costCentre = "CC-1001" });
        (await Client.PostAsync("/api/periods/2026-06/close", content: null)).EnsureSuccessStatusCode();

        var response = await Client.PostAsync("/api/erp/exports/2026-06", content: null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await ReadAsync<ErpExportDto>(response);
    }

    private static decimal SumOf(IEnumerable<string[]> rows, string product) =>
        rows.Where(row => row[3] == product).Sum(row => decimal.Parse(row[4], CultureInfo.InvariantCulture));
}

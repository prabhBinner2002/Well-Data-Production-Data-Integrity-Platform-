using System.Net;
using WellData.Api.Dtos;
using WellData.Tests.Integration.Infrastructure;

namespace WellData.Tests.Integration.Api;

public class WellsEndpointTests(SqlServerFixture sqlServer) : ApiTest(sqlServer)
{
    [Fact]
    public async Task List_returns_wells_in_id_order_with_the_total_count()
    {
        await SeedFixtureDataAsync();

        var page = await GetAsync<PagedResult<WellSummaryDto>>("/api/wells");

        Assert.Equal(9, page.TotalCount);
        Assert.Equal(9, page.Items.Count);
        Assert.Equal(page.Items.Select(well => well.WellId).Order(), page.Items.Select(well => well.WellId));
        Assert.Equal("100/01-36-049-05W5/00", page.Items[0].Uwi);
    }

    [Fact]
    public async Task List_pages_with_page_and_pageSize()
    {
        await SeedFixtureDataAsync();

        var first = await GetAsync<PagedResult<WellSummaryDto>>("/api/wells?page=1&pageSize=4");
        var last = await GetAsync<PagedResult<WellSummaryDto>>("/api/wells?page=3&pageSize=4");

        Assert.Equal(4, first.Items.Count);
        Assert.Single(last.Items);
        Assert.Equal(9, last.TotalCount);
        Assert.Equal(3, last.Page);
        Assert.DoesNotContain(last.Items[0].WellId, first.Items.Select(well => well.WellId));
    }

    [Theory]
    [InlineData("operatorBaId=A04N", 1)]
    [InlineData("operatorBaId=0XZ4", 8)]
    [InlineData("facilityId=ABBT0131180", 5)]
    [InlineData("search=LA%20GLACE", 2)]
    [InlineData("search=100%2F01-36-049-05W5", 1)]
    [InlineData("operatorBaId=0XZ4&facilityId=ABBT0170649", 3)]
    [InlineData("operatorBaId=0HE9", 0)]
    public async Task List_filters_by_operator_facility_and_search(string query, int expectedCount)
    {
        await SeedFixtureDataAsync();

        var page = await GetAsync<PagedResult<WellSummaryDto>>($"/api/wells?{query}");

        Assert.Equal(expectedCount, page.TotalCount);
    }

    [Theory]
    [InlineData("pageSize=0")]
    [InlineData("pageSize=201")]
    [InlineData("page=0")]
    [InlineData("page=abc")]
    public async Task List_rejects_invalid_paging_with_problem_details(string query)
    {
        var response = await Client.GetAsync($"/api/wells?{query}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        AssertProblemDetails(response);
    }

    [Fact]
    public async Task Detail_includes_linked_facilities_and_monthly_production()
    {
        await SeedFixtureDataAsync();

        var well = await GetAsync<WellDetailDto>("/api/wells/ABWI100013604905W500");

        Assert.Equal("BONTERRA HZ PEMBINA 1-36-49-5", well.Name);
        Assert.Equal("0479080", well.LicenceNumber);
        var facility = Assert.Single(well.Facilities);
        Assert.Equal("ABBT0131180", facility.FacilityId);
        var month = Assert.Single(well.MonthlyProduction);
        Assert.Equal(IngestionHarness.FixtureMonth, month.ProductionMonth);
        Assert.Equal(38.6m, month.OilVolume);
        Assert.Equal(9.8m, month.GasVolume);
        Assert.Equal(720, month.ProducingHours);
    }

    [Fact]
    public async Task Detail_of_an_unknown_well_is_404_problem_details()
    {
        var response = await Client.GetAsync("/api/wells/ABWI000000000000W000");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        AssertProblemDetails(response);
    }
}

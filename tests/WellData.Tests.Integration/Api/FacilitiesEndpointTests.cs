using System.Net;
using WellData.Api.Dtos;
using WellData.Tests.Integration.Infrastructure;

namespace WellData.Tests.Integration.Api;

public class FacilitiesEndpointTests(SqlServerFixture sqlServer) : ApiTest(sqlServer)
{
    [Fact]
    public async Task List_returns_facilities_with_their_operator()
    {
        await SeedFixtureDataAsync();

        var page = await GetAsync<PagedResult<FacilitySummaryDto>>("/api/facilities");

        Assert.Equal(3, page.TotalCount);
        var battery = page.Items.Single(facility => facility.FacilityId == "ABBT0175610");
        Assert.Equal("A04N", battery.OperatorBaId);
        Assert.Equal("TAMARACK VALLEY ENERGY LTD.", battery.OperatorName);
        Assert.Equal("BT", battery.FacilityType);
    }

    [Theory]
    [InlineData("operatorBaId=0XZ4", 2)]
    [InlineData("operatorBaId=A04N", 1)]
    [InlineData("facilityType=BT", 3)]
    [InlineData("facilityType=GP", 0)]
    [InlineData("pageSize=2&page=2", 3)]
    public async Task List_filters_by_operator_and_type(string query, int expectedTotal)
    {
        await SeedFixtureDataAsync();

        var page = await GetAsync<PagedResult<FacilitySummaryDto>>($"/api/facilities?{query}");

        Assert.Equal(expectedTotal, page.TotalCount);
    }

    [Fact]
    public async Task List_rejects_a_filter_value_that_is_too_long()
    {
        var response = await Client.GetAsync("/api/facilities?facilityType=BATTERY");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        AssertProblemDetails(response);
    }

    [Fact]
    public async Task Detail_lists_the_linked_wells()
    {
        await SeedFixtureDataAsync();

        var facility = await GetAsync<FacilityDetailDto>("/api/facilities/ABBT0131180");

        Assert.Equal("PEMBINA KEYSTONE 03-01-050-05W5", facility.Name);
        Assert.Equal("0XZ4", facility.OperatorBaId);
        Assert.Equal(5, facility.Wells.Count);
        Assert.Contains(facility.Wells, well => well is { WellId: "ABWI100013604905W500", Uwi: "100/01-36-049-05W5/00" });
    }

    [Fact]
    public async Task Detail_of_an_unknown_facility_is_404()
    {
        var response = await Client.GetAsync("/api/facilities/ABBT9999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        AssertProblemDetails(response);
    }
}

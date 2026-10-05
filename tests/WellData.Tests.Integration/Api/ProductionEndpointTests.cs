using System.Net;
using WellData.Api.Dtos;
using WellData.Tests.Integration.Infrastructure;

namespace WellData.Tests.Integration.Api;

public class ProductionEndpointTests(SqlServerFixture sqlServer) : ApiTest(sqlServer)
{
    [Fact]
    public async Task List_returns_one_row_per_producing_well_per_month()
    {
        await SeedFixtureDataAsync();

        var page = await GetAsync<PagedResult<MonthlyProductionDto>>("/api/production");

        Assert.Equal(12, page.TotalCount);
        var well = page.Items.Single(row => row.WellId == "ABWI100040307406W600");
        Assert.Equal(312.5m, well.OilVolume);
        Assert.Equal(525.5m, well.GasVolume);
        Assert.Equal(1052.9m, well.WaterVolume);
        Assert.Equal(707, well.ProducingHours);
    }

    [Theory]
    [InlineData("wellId=ABWI100013604905W500", 1)]
    [InlineData("operatorBaId=A04N", 5)]
    [InlineData("operatorBaId=0XZ4", 7)]
    [InlineData("fromMonth=2026-06&toMonth=2026-06", 12)]
    [InlineData("fromMonth=2026-07", 0)]
    [InlineData("toMonth=2026-05", 0)]
    [InlineData("operatorBaId=0XZ4&fromMonth=2026-01&toMonth=2026-12&pageSize=3", 7)]
    public async Task List_filters_by_well_operator_and_month_range(string query, int expectedTotal)
    {
        await SeedFixtureDataAsync();

        var page = await GetAsync<PagedResult<MonthlyProductionDto>>($"/api/production?{query}");

        Assert.Equal(expectedTotal, page.TotalCount);
    }

    [Theory]
    [InlineData("fromMonth=2026-13")]
    [InlineData("fromMonth=June")]
    [InlineData("fromMonth=2026-07&toMonth=2026-06")]
    public async Task List_rejects_an_invalid_month_or_reversed_range(string query)
    {
        var response = await Client.GetAsync($"/api/production?{query}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        AssertProblemDetails(response);
    }

    [Fact]
    public async Task Masked_well_is_marked_and_its_hours_are_null()
    {
        await SeedFixtureDataAsync();

        var page = await GetAsync<PagedResult<MonthlyProductionDto>>("/api/production?wellId=ABWI100163606223W400");

        var row = Assert.Single(page.Items);
        Assert.True(row.IsMasked);
        Assert.Null(row.ProducingHours);
        Assert.Equal(206.9m, row.OilVolume);
    }
}

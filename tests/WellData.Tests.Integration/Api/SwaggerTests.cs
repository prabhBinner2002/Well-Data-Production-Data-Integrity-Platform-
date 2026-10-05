using WellData.Tests.Integration.Infrastructure;

namespace WellData.Tests.Integration.Api;

public class SwaggerTests(SqlServerFixture sqlServer) : ApiTest(sqlServer)
{
    [Fact]
    public async Task OpenApi_document_describes_every_resource()
    {
        var document = await Client.GetStringAsync("/swagger/v1/swagger.json");

        Assert.Contains("/api/wells", document);
        Assert.Contains("/api/facilities", document);
        Assert.Contains("/api/production", document);
        Assert.Contains("/api/issues/{id}/resolve", document);
        Assert.Contains("/api/rules/{code}/enabled", document);
        Assert.Contains("/api/imports", document);
        Assert.Contains("/api/kpis/acceptance-rate", document);
    }

    [Fact]
    public async Task Swagger_ui_is_served()
    {
        var response = await Client.GetAsync("/swagger/index.html");

        response.EnsureSuccessStatusCode();
    }
}

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using WellData.Api.Auth;
using WellData.Api.Dtos;
using WellData.Core.Entities;
using WellData.Core.Validation;
using WellData.Tests.Integration.Infrastructure;

namespace WellData.Tests.Integration.Api;

public class AuthorizationTests(SqlServerFixture sqlServer) : ApiTest(sqlServer)
{
    public static TheoryData<string> ReadEndpoints =>
    [
        "/api/wells",
        "/api/wells/ABWI100013604905W500",
        "/api/facilities",
        "/api/facilities/ABBT0131180",
        "/api/production",
        "/api/issues",
        "/api/rules",
        "/api/imports",
        "/api/kpis/acceptance-rate",
        "/api/kpis/issues",
        "/api/kpis/production-totals",
    ];

    [Theory]
    [MemberData(nameof(ReadEndpoints))]
    public async Task Request_without_a_token_is_401(string url)
    {
        var response = await CreateAnonymousClient().GetAsync(url);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [MemberData(nameof(ReadEndpoints))]
    public async Task Viewer_can_read_everything(string url)
    {
        await SeedFixtureDataAsync();
        var viewer = await CreateClientAsync(Roles.Viewer);

        var response = await viewer.GetAsync(url);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Viewer_cannot_acknowledge_or_resolve_an_issue()
    {
        await SeedFixtureDataAsync();
        var issue = (await GetAsync<PagedResult<IssueDto>>("/api/issues?pageSize=1")).Items[0];
        var viewer = await CreateClientAsync(Roles.Viewer);

        var acknowledge = await viewer.PostAsJsonAsync($"/api/issues/{issue.Id}/acknowledge", new { rowVersion = issue.RowVersion });
        var resolve = await viewer.PostAsJsonAsync($"/api/issues/{issue.Id}/resolve", new { note = "Not allowed.", rowVersion = issue.RowVersion });

        Assert.Equal(HttpStatusCode.Forbidden, acknowledge.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, resolve.StatusCode);
        Assert.Equal(IssueStatus.Open, (await GetAsync<IssueDto>($"/api/issues/{issue.Id}")).Status);
    }

    [Fact]
    public async Task Viewer_cannot_change_a_rule()
    {
        var rule = (await GetAsync<List<RuleDto>>("/api/rules")).Single(candidate => candidate.Code == RuleCatalog.Spike);
        var viewer = await CreateClientAsync(Roles.Viewer);

        var response = await viewer.PutAsJsonAsync($"/api/rules/{rule.Code}/enabled", new { isEnabled = false, rowVersion = rule.RowVersion });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.True((await GetAsync<List<RuleDto>>("/api/rules")).Single(candidate => candidate.Code == RuleCatalog.Spike).IsEnabled);
    }

    [Fact]
    public async Task Viewer_cannot_trigger_an_import()
    {
        var viewer = await CreateClientAsync(Roles.Viewer);

        var response = await viewer.PostAsJsonAsync("/api/imports", new { month = "2026-06" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(0, (await GetAsync<PagedResult<ImportRunDto>>("/api/imports")).TotalCount);
    }

    [Fact]
    public async Task Anonymous_caller_cannot_write_either()
    {
        var response = await CreateAnonymousClient().PostAsJsonAsync("/api/imports", new { month = "2026-06" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Token_endpoint_returns_a_token_with_the_accounts_role()
    {
        var response = await CreateAnonymousClient().PostAsJsonAsync(
            "/api/auth/token",
            new { userName = ApiFactory.ViewerUserName, password = Factory.ViewerPassword });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var token = await ReadAsync<TokenResponse>(response);
        Assert.Equal(Roles.Viewer, token.Role);
        Assert.True(token.ExpiresAt > DateTime.UtcNow);
        Assert.Equal(3, token.AccessToken.Split('.').Length);
    }

    [Theory]
    [InlineData(ApiFactory.StewardUserName, "wrong-password")]
    [InlineData("nobody", "wrong-password")]
    public async Task Wrong_credentials_are_401_with_the_same_response(string userName, string password)
    {
        var response = await CreateAnonymousClient().PostAsJsonAsync("/api/auth/token", new { userName, password });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        AssertProblemDetails(response);
    }

    [Fact]
    public async Task Token_request_without_a_password_is_400()
    {
        var response = await CreateAnonymousClient().PostAsJsonAsync("/api/auth/token", new { userName = ApiFactory.StewardUserName });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Token_that_was_not_signed_by_this_api_is_401()
    {
        var client = CreateAnonymousClient();
        var genuine = await ReadAsync<TokenResponse>(await client.PostAsJsonAsync(
            "/api/auth/token",
            new { userName = ApiFactory.ViewerUserName, password = Factory.ViewerPassword }));
        var parts = genuine.AccessToken.Split('.');
        var forged = $"{parts[0]}.{parts[1]}.{new string('A', parts[2].Length)}";
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", forged);

        var response = await client.GetAsync("/api/wells");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Health_check_is_open_and_reports_the_database()
    {
        var response = await CreateAnonymousClient().GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }
}

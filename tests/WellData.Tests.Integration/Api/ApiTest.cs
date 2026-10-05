using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using WellData.Api.Auth;
using WellData.Api.Dtos;
using WellData.Api.Json;
using WellData.Tests.Integration.Infrastructure;

namespace WellData.Tests.Integration.Api;

/// <summary>Base class for endpoint tests: a clean database, the API hosted in memory, and HTTP clients per role.</summary>
public abstract class ApiTest(SqlServerFixture sqlServer) : DatabaseTest(sqlServer)
{
    protected static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new ProductionMonthJsonConverter(), new JsonStringEnumConverter() },
    };

    protected IngestionHarness Harness { get; private set; } = null!;

    protected ApiFactory Factory { get; private set; } = null!;

    /// <summary>Signed in as a DataSteward, who may call every endpoint.</summary>
    protected HttpClient Client { get; private set; } = null!;

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        Harness = new IngestionHarness(Database);
        Factory = CreateFactory();
        Client = await CreateClientAsync(Roles.DataSteward);
    }

    public override async Task DisposeAsync()
    {
        await Factory.DisposeAsync();
        await base.DisposeAsync();
    }

    protected virtual ApiFactory CreateFactory() => new(Database, Harness.Petrinex);

    protected HttpClient CreateAnonymousClient() => Factory.CreateClient();

    /// <summary>Signs in through the real token endpoint and returns a client that sends the bearer token.</summary>
    protected async Task<HttpClient> CreateClientAsync(string role)
    {
        var (userName, password) = role == Roles.DataSteward
            ? (ApiFactory.StewardUserName, Factory.StewardPassword)
            : (ApiFactory.ViewerUserName, Factory.ViewerPassword);

        var client = Factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/token", new { userName, password });
        response.EnsureSuccessStatusCode();
        var token = await ReadAsync<TokenResponse>(response);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);
        return client;
    }

    /// <summary>Loads the master data fixtures and the clean June 2026 volumetric fixture.</summary>
    protected async Task SeedFixtureDataAsync()
    {
        await Harness.IngestMasterAsync();
        await Harness.IngestFixtureMonthAsync();
    }

    protected async Task<T> GetAsync<T>(string url)
    {
        var response = await Client.GetAsync(url);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<T>(Json))!;
    }

    protected static async Task<T> ReadAsync<T>(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<T>(Json))!;

    protected static void AssertProblemDetails(HttpResponseMessage response) =>
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
}

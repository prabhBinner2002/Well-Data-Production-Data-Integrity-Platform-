using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using WellData.Api.Auth;
using WellData.Infrastructure.Petrinex;

namespace WellData.Tests.Integration.Infrastructure;

/// <summary>Hosts the real API in memory against the test database, with Petrinex replaced by fixture files.</summary>
public sealed class ApiFactory(TestDatabase database, FixturePetrinexClient petrinex) : WebApplicationFactory<Program>
{
    public const string StewardUserName = "steward";
    public const string ViewerUserName = "viewer";

    // Generated per test run so no credential or signing key is ever written to the repository.
    private readonly string _signingKey = $"{Guid.NewGuid():N}{Guid.NewGuid():N}";

    public string StewardPassword { get; } = Guid.NewGuid().ToString("N");

    public string ViewerPassword { get; } = Guid.NewGuid().ToString("N");

    /// <summary>Where the hosted API writes ERP export files.</summary>
    public string ExportDirectory { get; } = Path.Combine(Path.GetTempPath(), $"welldata-exports-{Guid.NewGuid():N}");

    /// <summary>Replaces the system clock, for tests that control time.</summary>
    public TimeProvider? Clock { get; init; }

    /// <summary>Extra configuration values, applied last.</summary>
    public Dictionary<string, string> Settings { get; init; } = [];

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:WellData", database.ConnectionString);
        builder.UseSetting("Jwt:SigningKey", _signingKey);
        builder.UseSetting("Auth:Users:0:UserName", StewardUserName);
        builder.UseSetting("Auth:Users:0:Password", StewardPassword);
        builder.UseSetting("Auth:Users:0:Role", Roles.DataSteward);
        builder.UseSetting("Auth:Users:1:UserName", ViewerUserName);
        builder.UseSetting("Auth:Users:1:Password", ViewerPassword);
        builder.UseSetting("Auth:Users:1:Role", Roles.Viewer);

        // Off by default so a background import never races with what a test is arranging.
        builder.UseSetting("IngestionScheduler:Enabled", "false");
        builder.UseSetting("ErpExport:Directory", ExportDirectory);

        foreach (var (key, value) in Settings)
        {
            builder.UseSetting(key, value);
        }

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IPetrinexClient>();
            services.AddSingleton<IPetrinexClient>(petrinex);

            if (Clock is not null)
            {
                services.RemoveAll<TimeProvider>();
                services.AddSingleton(Clock);
            }
        });
    }
}

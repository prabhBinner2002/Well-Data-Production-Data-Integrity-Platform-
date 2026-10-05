using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace WellData.Infrastructure.Persistence;

public static class DatabaseMigrator
{
    public const string MigrateOnStartupKey = "Database:MigrateOnStartup";

    /// <summary>
    /// Applies pending migrations when Database:MigrateOnStartup is true. Used by Docker Compose, where there is
    /// no separate deployment step. It is off by default so a normal run never changes the schema by surprise.
    /// </summary>
    public static async Task MigrateIfConfiguredAsync(IServiceProvider services, IConfiguration configuration, CancellationToken cancellationToken)
    {
        if (!configuration.GetValue<bool>(MigrateOnStartupKey))
        {
            return;
        }

        await using var scope = services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<WellDataDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<WellDataDbContext>>();

        logger.LogInformation("Applying database migrations");
        await context.Database.MigrateAsync(cancellationToken);
    }
}

using Microsoft.Extensions.Diagnostics.HealthChecks;
using WellData.Infrastructure.Persistence;

namespace WellData.Api.Services;

/// <summary>Reports healthy when SQL Server accepts a connection to the application database.</summary>
public sealed class DatabaseHealthCheck(WellDataDbContext context) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext healthCheckContext, CancellationToken cancellationToken = default) =>
        await context.Database.CanConnectAsync(cancellationToken)
            ? HealthCheckResult.Healthy()
            : HealthCheckResult.Unhealthy("Cannot connect to the database.");
}

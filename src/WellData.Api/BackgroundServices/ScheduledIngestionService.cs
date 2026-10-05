using Microsoft.Extensions.Options;
using WellData.Infrastructure.Scheduling;

namespace WellData.Api.BackgroundServices;

/// <summary>Runs the month check once at start-up and then on a fixed interval for as long as the app runs.</summary>
public sealed class ScheduledIngestionService(
    IServiceScopeFactory scopeFactory,
    IOptions<IngestionSchedulerOptions> options,
    TimeProvider timeProvider,
    ILogger<ScheduledIngestionService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.Enabled)
        {
            logger.LogInformation("Scheduled ingestion is disabled");
            return;
        }

        using var timer = new PeriodicTimer(options.Value.Interval, timeProvider);

        do
        {
            await RunCheckAsync(stoppingToken);
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task RunCheckAsync(CancellationToken stoppingToken)
    {
        try
        {
            // The checker and its DbContext are scoped, and a hosted service is a singleton, so each run gets its own scope.
            await using var scope = scopeFactory.CreateAsyncScope();
            await scope.ServiceProvider.GetRequiredService<ScheduledMonthChecker>().CheckAsync(stoppingToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // One bad run (database down, Petrinex unreachable) must not stop future runs.
            logger.LogError(exception, "Scheduled ingestion check failed");
        }
    }
}

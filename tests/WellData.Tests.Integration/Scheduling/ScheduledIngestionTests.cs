using Microsoft.EntityFrameworkCore;
using WellData.Core;
using WellData.Core.Entities;
using WellData.Infrastructure.Ingestion.Volumetric;
using WellData.Tests.Integration.Infrastructure;

namespace WellData.Tests.Integration.Scheduling;

public class ScheduledIngestionTests(SqlServerFixture sqlServer) : DatabaseTest(sqlServer)
{
    private static readonly DateTimeOffset MidAugust = new(2026, 8, 15, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Check_loads_published_months_in_the_window_and_ignores_unpublished_ones()
    {
        var harness = CreateHarness(MidAugust, lookbackMonths: 3);

        var results = await harness.RunScheduledCheckAsync();

        // The window is May to July. Only June has a file.
        var june = Assert.Single(results);
        Assert.Equal(IngestionHarness.FixtureMonth, june.Month);
        Assert.Equal(ImportOutcome.Loaded, june.Outcome);
        await using var context = Database.CreateContext();
        Assert.Equal(124, await context.VolumetricRecords.CountAsync());
    }

    [Fact]
    public async Task Unchanged_months_are_skipped_on_the_next_check()
    {
        var harness = CreateHarness(MidAugust, lookbackMonths: 3);
        await harness.RunScheduledCheckAsync();

        var second = await harness.RunScheduledCheckAsync();

        Assert.Equal(ImportOutcome.Unchanged, Assert.Single(second).Outcome);
        await using var context = Database.CreateContext();
        Assert.Equal(1, await context.ImportRuns.CountAsync());
        Assert.Equal(1, await context.SourceFiles.CountAsync());
        Assert.Equal(124, await context.VolumetricRecords.CountAsync());
    }

    [Fact]
    public async Task Month_whose_file_changed_is_loaded_again()
    {
        var harness = CreateHarness(MidAugust, lookbackMonths: 3);
        await harness.RunScheduledCheckAsync();
        harness.Petrinex.SetVolumetricFile(IngestionHarness.FixtureMonth, FixtureFiles.PathTo(FixtureFiles.VolumetricRestated));

        var second = await harness.RunScheduledCheckAsync();

        Assert.Equal(ImportOutcome.Loaded, Assert.Single(second).Outcome);
        await using var context = Database.CreateContext();
        Assert.Equal(123, await context.VolumetricRecords.CountAsync());
        Assert.Equal(1, await context.SourceFiles.CountAsync(file => file.Status == SourceFileStatus.Superseded));
    }

    [Fact]
    public async Task Month_outside_the_window_is_not_touched()
    {
        var clock = new ManualTimeProvider(MidAugust);
        var harness = CreateHarness(clock, lookbackMonths: 1);

        Assert.Empty(await harness.RunScheduledCheckAsync());

        // In July the window is June, which has a file.
        clock.SetNow(new DateTimeOffset(2026, 7, 2, 0, 0, 0, TimeSpan.Zero));
        Assert.Single(await harness.RunScheduledCheckAsync());
    }

    [Fact]
    public async Task Months_are_loaded_oldest_first()
    {
        var harness = CreateHarness(MidAugust, lookbackMonths: 3);
        var july = new ProductionMonth(2026, 7);
        harness.Petrinex.SetVolumetricFile(july, FixtureEditor.CopyWith(FixtureFiles.Volumetric, lines =>
            lines.Select((line, index) => index == 0 ? line : "2026-07" + line[7..])));

        var results = await harness.RunScheduledCheckAsync();

        Assert.Equal([IngestionHarness.FixtureMonth, july], results.Select(result => result.Month));
        Assert.All(results, result => Assert.Equal(ImportOutcome.Loaded, result.Outcome));
    }

    [Fact]
    public async Task Background_service_loads_new_data_at_start_up_without_any_manual_step()
    {
        var harness = new IngestionHarness(Database);
        await using var factory = new ApiFactory(Database, harness.Petrinex)
        {
            Clock = new ManualTimeProvider(MidAugust),
            Settings =
            {
                ["IngestionScheduler:Enabled"] = "true",
                ["IngestionScheduler:LookbackMonths"] = "3",
            },
        };

        // Creating a client starts the host, which starts the hosted service.
        using var client = factory.CreateClient();

        Assert.True(await WaitForAsync(async () =>
        {
            await using var context = Database.CreateContext();
            return await context.ImportRuns.AnyAsync(run => run.Status == ImportRunStatus.Succeeded);
        }));
        await using var verify = Database.CreateContext();
        Assert.Equal(124, await verify.VolumetricRecords.CountAsync());
    }

    private IngestionHarness CreateHarness(DateTimeOffset now, int lookbackMonths) =>
        CreateHarness(new ManualTimeProvider(now), lookbackMonths);

    private IngestionHarness CreateHarness(TimeProvider clock, int lookbackMonths)
    {
        var harness = new IngestionHarness(Database) { Clock = clock };
        harness.Scheduler.LookbackMonths = lookbackMonths;
        return harness;
    }

    private static async Task<bool> WaitForAsync(Func<Task<bool>> condition)
    {
        var deadline = DateTime.UtcNow.AddSeconds(30);
        while (DateTime.UtcNow < deadline)
        {
            if (await condition())
            {
                return true;
            }

            await Task.Delay(200);
        }

        return false;
    }
}

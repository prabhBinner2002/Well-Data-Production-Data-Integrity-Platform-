using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using WellData.Core;
using WellData.Infrastructure.Scheduling;

namespace WellData.Tests.Unit.Scheduling;

public class ScheduledMonthCheckerTests
{
    [Fact]
    public void Window_is_the_complete_months_before_the_current_one_oldest_first()
    {
        var checker = CreateChecker(new DateTimeOffset(2026, 8, 15, 9, 0, 0, TimeSpan.Zero), lookbackMonths: 3);

        Assert.Equal(
            [new ProductionMonth(2026, 5), new ProductionMonth(2026, 6), new ProductionMonth(2026, 7)],
            checker.MonthsToCheck());
    }

    [Fact]
    public void Window_crosses_the_year_boundary()
    {
        var checker = CreateChecker(new DateTimeOffset(2026, 2, 1, 0, 0, 0, TimeSpan.Zero), lookbackMonths: 3);

        Assert.Equal(
            [new ProductionMonth(2025, 11), new ProductionMonth(2025, 12), new ProductionMonth(2026, 1)],
            checker.MonthsToCheck());
    }

    [Fact]
    public void Window_moves_forward_as_time_passes()
    {
        var clock = new ManualTimeProvider(new DateTimeOffset(2026, 8, 31, 23, 59, 0, TimeSpan.Zero));
        var checker = CreateChecker(clock, lookbackMonths: 1);
        Assert.Equal([new ProductionMonth(2026, 7)], checker.MonthsToCheck());

        clock.Advance(TimeSpan.FromMinutes(2));

        Assert.Equal([new ProductionMonth(2026, 8)], checker.MonthsToCheck());
    }

    [Fact]
    public void The_current_incomplete_month_is_never_checked()
    {
        var checker = CreateChecker(new DateTimeOffset(2026, 8, 15, 9, 0, 0, TimeSpan.Zero), lookbackMonths: 12);

        Assert.DoesNotContain(new ProductionMonth(2026, 8), checker.MonthsToCheck());
        Assert.Equal(12, checker.MonthsToCheck().Count());
    }

    private static ScheduledMonthChecker CreateChecker(DateTimeOffset now, int lookbackMonths) =>
        CreateChecker(new ManualTimeProvider(now), lookbackMonths);

    // The ingestion service is not needed to compute the window, so it is left out.
    private static ScheduledMonthChecker CreateChecker(TimeProvider clock, int lookbackMonths) =>
        new(
            ingestion: null!,
            Options.Create(new IngestionSchedulerOptions { LookbackMonths = lookbackMonths }),
            clock,
            NullLogger<ScheduledMonthChecker>.Instance);
}

using System.ComponentModel.DataAnnotations;

namespace WellData.Infrastructure.Scheduling;

public sealed class IngestionSchedulerOptions
{
    public const string SectionName = "IngestionScheduler";

    public bool Enabled { get; set; } = true;

    /// <summary>Time between checks. Petrinex publishes monthly, so a few hours is frequent enough.</summary>
    [Range(typeof(TimeSpan), "00:01:00", "7.00:00:00")]
    public TimeSpan Interval { get; set; } = TimeSpan.FromHours(6);

    /// <summary>How many months before the current one to check on every run.</summary>
    [Range(1, 60)]
    public int LookbackMonths { get; set; } = 4;
}

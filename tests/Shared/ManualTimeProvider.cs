namespace WellData.Tests;

/// <summary>A clock that only moves when a test tells it to.</summary>
public sealed class ManualTimeProvider(DateTimeOffset now) : TimeProvider
{
    public DateTimeOffset Now { get; private set; } = now;

    public override DateTimeOffset GetUtcNow() => Now;

    public void Advance(TimeSpan duration) => Now += duration;

    public void SetNow(DateTimeOffset now) => Now = now;
}

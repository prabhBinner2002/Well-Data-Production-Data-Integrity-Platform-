namespace WellData.Infrastructure.Auditing;

/// <summary>Who is making the current change. Outside an HTTP request this is the system itself.</summary>
public interface ICurrentUser
{
    string Name { get; }
}

public sealed class SystemUser : ICurrentUser
{
    public const string SystemName = "system";

    public string Name => SystemName;
}

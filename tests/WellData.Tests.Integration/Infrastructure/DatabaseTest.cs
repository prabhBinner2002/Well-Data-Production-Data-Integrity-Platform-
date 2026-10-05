namespace WellData.Tests.Integration.Infrastructure;

/// <summary>Base class for tests that need their own empty, migrated database.</summary>
[Collection(SqlServerCollection.Name)]
public abstract class DatabaseTest(SqlServerFixture sqlServer) : IAsyncLifetime
{
    protected TestDatabase Database { get; private set; } = null!;

    public virtual async Task InitializeAsync() => Database = await sqlServer.CreateDatabaseAsync();

    public virtual Task DisposeAsync() => Task.CompletedTask;
}

using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Testcontainers.MsSql;
using WellData.Infrastructure.Persistence;

namespace WellData.Tests.Integration.Infrastructure;

/// <summary>
/// Starts one SQL Server container for the whole test run and gives each test a clean, migrated database.
/// Migrations run once, then a database snapshot is taken. Before each test the database is reverted to
/// that snapshot, which is much faster than creating and migrating a new database every time.
/// Tests in the collection run one at a time, so sharing a single database is safe.
/// </summary>
public sealed class SqlServerFixture : IAsyncLifetime
{
    private const string DatabaseName = "WellData_Test";
    private const string SnapshotName = "WellData_Test_Snapshot";

    private readonly MsSqlContainer _container = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        await using (var context = new TestDatabase(ConnectionStringFor(DatabaseName)).CreateContext())
        {
            await context.Database.MigrateAsync();
        }

        await ExecuteOnMasterAsync($"""
            CREATE DATABASE [{SnapshotName}]
            ON (NAME = '{DatabaseName}', FILENAME = '/var/opt/mssql/data/{SnapshotName}.ss')
            AS SNAPSHOT OF [{DatabaseName}];
            """);
    }

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();

    public async Task<TestDatabase> CreateDatabaseAsync()
    {
        // A revert needs exclusive access, so pooled connections left by the previous test are dropped first.
        SqlConnection.ClearAllPools();

        await ExecuteOnMasterAsync($"""
            ALTER DATABASE [{DatabaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
            RESTORE DATABASE [{DatabaseName}] FROM DATABASE_SNAPSHOT = '{SnapshotName}';
            ALTER DATABASE [{DatabaseName}] SET MULTI_USER;
            """);

        return new TestDatabase(ConnectionStringFor(DatabaseName));
    }

    private string ConnectionStringFor(string databaseName) =>
        new SqlConnectionStringBuilder(_container.GetConnectionString()) { InitialCatalog = databaseName }.ConnectionString;

    private async Task ExecuteOnMasterAsync(string sql)
    {
        await using var connection = new SqlConnection(ConnectionStringFor("master"));
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync();
    }
}
[CollectionDefinition(Name)]
public sealed class SqlServerCollection : ICollectionFixture<SqlServerFixture>
{
    public const string Name = "SqlServer";
}

public sealed class TestDatabase(string connectionString)
{
    public string ConnectionString { get; } = connectionString;

    public WellDataDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<WellDataDbContext>().UseSqlServer(ConnectionString).Options);
}

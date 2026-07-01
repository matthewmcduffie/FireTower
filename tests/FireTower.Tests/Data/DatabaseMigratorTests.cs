using FireTower.Data.Migrations;
using FireTower.Data.Services;
using Microsoft.Data.Sqlite;

namespace FireTower.Tests.Data;

/// <summary>
/// Integration tests for <see cref="DatabaseMigrator"/>. Uses an in-memory SQLite
/// database (no files on disk) per testing.md's "use disposable SQLite databases."
/// </summary>
public sealed class DatabaseMigratorTests
{
    private sealed class InMemoryConnectionFactory : ISqliteConnectionFactory, IDisposable
    {
        // A named shared-cache in-memory database lets the migrator create (and dispose)
        // individual connections without destroying the database, as long as this keeper
        // connection stays open for the lifetime of the test.
        private readonly string _connectionString;
        private readonly SqliteConnection _keeperConnection;

        public InMemoryConnectionFactory()
        {
            var dbName = $"test_{Guid.NewGuid():N}";
            _connectionString = $"Data Source={dbName};Mode=Memory;Cache=Shared";
            _keeperConnection = new SqliteConnection(_connectionString);
            _keeperConnection.Open();
        }

        public SqliteConnection CreateConnection()
        {
            var connection = new SqliteConnection(_connectionString);
            connection.Open();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "PRAGMA foreign_keys = ON;";
            cmd.ExecuteNonQuery();
            return connection;
        }

        public void Dispose() => _keeperConnection.Dispose();
    }

    [Fact]
    public void Migrate_CreatesAllExpectedTables()
    {
        using var factory = new InMemoryConnectionFactory();
        var migrator = new DatabaseMigrator(factory, Microsoft.Extensions.Logging.Abstractions.NullLogger<DatabaseMigrator>.Instance);

        migrator.Migrate();

        using var connection = factory.CreateConnection();
        var tables = GetTableNames(connection);

        Assert.Contains("VirtualMachines", tables);
        Assert.Contains("HealthHistory", tables);
        Assert.Contains("RestartHistory", tables);
        Assert.Contains("Events", tables);
        Assert.Contains("StatisticsSnapshots", tables);
    }

    [Fact]
    public void Migrate_SetsUserVersionToLatestMigrationNumber()
    {
        using var factory = new InMemoryConnectionFactory();
        var migrator = new DatabaseMigrator(factory, Microsoft.Extensions.Logging.Abstractions.NullLogger<DatabaseMigrator>.Instance);

        migrator.Migrate();

        using var connection = factory.CreateConnection();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "PRAGMA user_version;";
        var version = Convert.ToInt32(cmd.ExecuteScalar());

        Assert.Equal(MigrationCatalog.All.Max(m => m.Version), version);
    }

    [Fact]
    public void Migrate_IsIdempotent_WhenRunTwice()
    {
        using var factory = new InMemoryConnectionFactory();
        var migrator = new DatabaseMigrator(factory, Microsoft.Extensions.Logging.Abstractions.NullLogger<DatabaseMigrator>.Instance);

        migrator.Migrate();
        var exception = Record.Exception(() => migrator.Migrate());

        Assert.Null(exception);
    }

    private static HashSet<string> GetTableNames(SqliteConnection connection)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table';";
        var tables = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            tables.Add(reader.GetString(0));
        }

        return tables;
    }
}

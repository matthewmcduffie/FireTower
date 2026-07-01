using FireTower.Data.Services;
using Microsoft.Extensions.Logging;

namespace FireTower.Data.Migrations;

/// <summary>
/// Applies pending schema migrations on startup, tracked via SQLite's built-in
/// <c>user_version</c> pragma. Never requires a manual database upgrade step.
/// </summary>
public sealed class DatabaseMigrator
{
    private readonly ISqliteConnectionFactory _connectionFactory;
    private readonly ILogger<DatabaseMigrator> _logger;

    public DatabaseMigrator(ISqliteConnectionFactory connectionFactory, ILogger<DatabaseMigrator> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public void Migrate()
    {
        using var connection = _connectionFactory.CreateConnection();

        var currentVersion = GetUserVersion(connection);

        foreach (var migration in MigrationCatalog.All.OrderBy(m => m.Version))
        {
            if (migration.Version <= currentVersion)
            {
                continue;
            }

            using var transaction = connection.BeginTransaction();
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = migration.Sql;
            command.ExecuteNonQuery();
            SetUserVersion(connection, transaction, migration.Version);
            transaction.Commit();

            _logger.LogInformation("Applied database migration {Version}: {Description}", migration.Version, migration.Description);
        }
    }

    private static int GetUserVersion(Microsoft.Data.Sqlite.SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA user_version;";
        return Convert.ToInt32(command.ExecuteScalar());
    }

    private static void SetUserVersion(Microsoft.Data.Sqlite.SqliteConnection connection, Microsoft.Data.Sqlite.SqliteTransaction transaction, int version)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $"PRAGMA user_version = {version};";
        command.ExecuteNonQuery();
    }
}

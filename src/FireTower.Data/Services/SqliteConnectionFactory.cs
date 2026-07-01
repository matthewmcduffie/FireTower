using System.Data;
using Dapper;
using FireTower.Core.Configuration.Paths;
using Microsoft.Data.Sqlite;

namespace FireTower.Data.Services;

/// <summary>
/// Default <see cref="ISqliteConnectionFactory"/> implementation. The database file lives
/// under the configurable data directory described in database.md, never inside
/// Program Files.
/// </summary>
public sealed class SqliteConnectionFactory : ISqliteConnectionFactory
{
    private readonly string _connectionString;

    static SqliteConnectionFactory()
    {
        // Dapper cannot convert the ISO 8601 strings SQLite stores back to DateTimeOffset
        // without guidance. Register a TypeHandler once per process so every query that
        // reads or writes DateTimeOffset values round-trips correctly.
        SqlMapper.AddTypeHandler(new DateTimeOffsetHandler());
    }

    public SqliteConnectionFactory(IFireTowerPaths paths)
    {
        Directory.CreateDirectory(paths.DataDirectory);
        var databasePath = Path.Combine(paths.DataDirectory, "firetower.db");
        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            Cache = SqliteCacheMode.Shared,
        }.ToString();
    }

    public SqliteConnection CreateConnection()
    {
        var connection = new SqliteConnection(_connectionString);
        connection.Open();
        using var pragma = connection.CreateCommand();
        pragma.CommandText = "PRAGMA foreign_keys = ON; PRAGMA journal_mode = WAL;";
        pragma.ExecuteNonQuery();
        return connection;
    }

    private sealed class DateTimeOffsetHandler : SqlMapper.TypeHandler<DateTimeOffset>
    {
        public override DateTimeOffset Parse(object value) =>
            DateTimeOffset.Parse((string)value, null, System.Globalization.DateTimeStyles.RoundtripKind);

        public override void SetValue(IDbDataParameter parameter, DateTimeOffset value) =>
            parameter.Value = value.ToString("O");
    }
}

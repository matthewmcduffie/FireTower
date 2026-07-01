using Microsoft.Data.Sqlite;

namespace FireTower.Data.Services;

/// <summary>
/// Creates SQLite connections to FireTower's database file.
/// </summary>
public interface ISqliteConnectionFactory
{
    SqliteConnection CreateConnection();
}

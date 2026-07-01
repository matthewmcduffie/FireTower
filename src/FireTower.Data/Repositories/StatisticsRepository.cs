using Dapper;
using FireTower.Core.Interfaces;
using FireTower.Core.Models;
using FireTower.Data.Entities;
using FireTower.Data.Services;

namespace FireTower.Data.Repositories;

/// <summary>
/// SQLite-backed implementation of <see cref="IStatisticsRepository"/>.
/// </summary>
public sealed class StatisticsRepository : IStatisticsRepository
{
    private readonly ISqliteConnectionFactory _connectionFactory;

    public StatisticsRepository(ISqliteConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task RecordAsync(StatisticsSnapshot snapshot, CancellationToken cancellationToken)
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync("""
            INSERT INTO StatisticsSnapshots
                (Timestamp, TotalVmCount, HealthyCount, WarningCount, CriticalCount, RestartCount,
                 AverageRestartDurationSeconds, AverageHealthCheckDurationMs)
            VALUES
                (@Timestamp, @TotalVmCount, @HealthyCount, @WarningCount, @CriticalCount, @RestartCount,
                 @AverageRestartDurationSeconds, @AverageHealthCheckDurationMs);
            """, snapshot).ConfigureAwait(false);
    }

    public async Task<StatisticsSnapshot?> GetLatestAsync(CancellationToken cancellationToken)
    {
        using var connection = _connectionFactory.CreateConnection();
        var row = await connection.QuerySingleOrDefaultAsync<StatisticsSnapshotEntity>(
            "SELECT * FROM StatisticsSnapshots ORDER BY Timestamp DESC LIMIT 1;").ConfigureAwait(false);

        return row is null ? null : ToModel(row);
    }

    public async Task PurgeOlderThanAsync(DateTimeOffset cutoff, CancellationToken cancellationToken)
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(
            "DELETE FROM StatisticsSnapshots WHERE Timestamp < @Cutoff;", new { Cutoff = cutoff }).ConfigureAwait(false);
    }

    private static StatisticsSnapshot ToModel(StatisticsSnapshotEntity entity) => new()
    {
        Timestamp = entity.Timestamp,
        TotalVmCount = entity.TotalVmCount,
        HealthyCount = entity.HealthyCount,
        WarningCount = entity.WarningCount,
        CriticalCount = entity.CriticalCount,
        RestartCount = entity.RestartCount,
        AverageRestartDurationSeconds = entity.AverageRestartDurationSeconds,
        AverageHealthCheckDurationMs = entity.AverageHealthCheckDurationMs,
    };
}

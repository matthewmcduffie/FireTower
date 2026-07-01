namespace FireTower.Data.Entities;

/// <summary>
/// Flat row shape of the StatisticsSnapshots table, mapped by Dapper.
/// </summary>
public sealed class StatisticsSnapshotEntity
{
    public long Id { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
    public required int TotalVmCount { get; init; }
    public required int HealthyCount { get; init; }
    public required int WarningCount { get; init; }
    public required int CriticalCount { get; init; }
    public required int RestartCount { get; init; }
    public required double AverageRestartDurationSeconds { get; init; }
    public required double AverageHealthCheckDurationMs { get; init; }
}

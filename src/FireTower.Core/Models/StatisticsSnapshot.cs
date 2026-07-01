namespace FireTower.Core.Models;

/// <summary>
/// A periodic summary of fleet-wide statistics, recorded for the tray application's
/// Statistics page.
/// </summary>
public sealed class StatisticsSnapshot
{
    public required DateTimeOffset Timestamp { get; init; }
    public required int TotalVmCount { get; init; }
    public required int HealthyCount { get; init; }
    public required int WarningCount { get; init; }
    public required int CriticalCount { get; init; }
    public required int RestartCount { get; init; }
    public required double AverageRestartDurationSeconds { get; init; }
    public required double AverageHealthCheckDurationMs { get; init; }
}

using FireTower.Shared.Enums;

namespace FireTower.Shared.DTOs;

/// <summary>
/// Snapshot of the Windows Service's own operational status, displayed on the tray dashboard.
/// </summary>
public sealed class ServiceStatusDto
{
    public required ServiceHealthState State { get; init; }
    public required DateTimeOffset StartedAt { get; init; }
    public required int MonitoredVmCount { get; init; }
    public required int HealthyVmCount { get; init; }
    public required int WarningVmCount { get; init; }
    public required int FailedVmCount { get; init; }
    public DateTimeOffset? LastConfigurationReload { get; init; }
    public required string Version { get; init; }
}

using FireTower.Shared.Enums;

namespace FireTower.Core.Services;

/// <summary>
/// Tracks whether monitoring is currently paused (PauseMonitoring/ResumeMonitoring in
/// ipc.md) and the service's own uptime, so both the monitoring loop and the IPC status
/// endpoint can read the same state.
/// </summary>
public sealed class MonitoringState
{
    private volatile bool _isPaused;

    public DateTimeOffset StartedAt { get; } = DateTimeOffset.UtcNow;

    public bool IsPaused => _isPaused;

    public void Pause() => _isPaused = true;

    public void Resume() => _isPaused = false;

    public ServiceHealthState CurrentServiceState => _isPaused ? ServiceHealthState.Paused : ServiceHealthState.Running;
}

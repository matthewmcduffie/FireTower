namespace FireTower.Shared.Enums;

/// <summary>
/// Operational state of the FireTower Windows Service itself, as displayed by the tray application.
/// </summary>
public enum ServiceHealthState
{
    Initializing,
    Running,
    Paused,
    Recovering,
    Stopping,
    Faulted,
}

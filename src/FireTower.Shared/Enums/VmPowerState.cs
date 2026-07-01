namespace FireTower.Shared.Enums;

/// <summary>
/// Provider-independent virtual machine power state. Providers translate their
/// native states into this set so the rest of FireTower never depends on
/// platform-specific terminology.
/// </summary>
public enum VmPowerState
{
    Unknown,
    Running,
    Stopped,
    Paused,
    Saved,
    Starting,
    Stopping,
    Restoring,
    Aborted,
    Inaccessible,
}

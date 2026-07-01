namespace FireTower.Shared.Enums;

/// <summary>
/// Action the Restart Engine may take in response to a health failure.
/// </summary>
public enum RecoveryAction
{
    DoNothing,
    LogOnly,
    Notify,
    GracefulShutdown,
    Restart,
    ForceRestart,
    PowerOff,
    Start,
}

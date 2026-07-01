namespace FireTower.Shared.Enums;

/// <summary>
/// Current state of the Restart Engine's recovery workflow for a single virtual machine.
/// </summary>
public enum RecoveryState
{
    Idle,
    Pending,
    Waiting,
    Recovering,
    Succeeded,
    Failed,
    Disabled,
    Maintenance,
    Cooldown,
}

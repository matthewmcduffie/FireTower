using FireTower.Shared.Enums;

namespace FireTower.Core.Models;

/// <summary>
/// A reusable recovery strategy: cooldowns, retry limits, escalation, and maintenance windows.
/// </summary>
public sealed class RecoveryProfile
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public bool PreferGracefulRestart { get; init; } = true;
    public int CooldownSeconds { get; init; } = 600;
    public int MaxRestartAttempts { get; init; } = 3;
    public int RetryWindowSeconds { get; init; } = 3600;
    public IReadOnlyList<RecoveryAction> EscalationSequence { get; init; } =
        new[] { RecoveryAction.Restart, RecoveryAction.ForceRestart, RecoveryAction.Notify };
    public TimeOnly? MaintenanceWindowStart { get; init; }
    public TimeOnly? MaintenanceWindowEnd { get; init; }
    public DayOfWeek? MaintenanceWindowDay { get; init; }
}

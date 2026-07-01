using FireTower.Shared.Enums;

namespace FireTower.Shared.DTOs;

/// <summary>
/// A reusable recovery strategy that may be shared by multiple virtual machines.
/// </summary>
public sealed class RecoveryProfileDto
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required bool PreferGracefulRestart { get; init; }
    public required int CooldownSeconds { get; init; }
    public required int MaxRestartAttempts { get; init; }
    public required int RetryWindowSeconds { get; init; }
    public IReadOnlyList<RecoveryAction> EscalationSequence { get; init; } =
        Array.Empty<RecoveryAction>();
    public TimeOnly? MaintenanceWindowStart { get; init; }
    public TimeOnly? MaintenanceWindowEnd { get; init; }
    public DayOfWeek? MaintenanceWindowDay { get; init; }
}

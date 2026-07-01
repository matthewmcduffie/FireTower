using FireTower.Shared.Enums;

namespace FireTower.Core.Models;

/// <summary>
/// The recorded outcome of executing a single recovery action.
/// </summary>
public sealed class RecoveryResult
{
    public required Guid VirtualMachineId { get; init; }
    public required RecoveryAction Action { get; init; }
    public required bool Success { get; init; }
    public RecoveryFailureCategory FailureCategory { get; init; } = RecoveryFailureCategory.None;
    public string? FailureReason { get; init; }
    public required TimeSpan Duration { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
    public required string CorrelationId { get; init; }
}

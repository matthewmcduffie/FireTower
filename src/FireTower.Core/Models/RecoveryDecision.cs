using FireTower.Shared.Enums;

namespace FireTower.Core.Models;

/// <summary>
/// The Restart Engine's decision about what to do in response to a health evaluation,
/// before the action is executed.
/// </summary>
public sealed class RecoveryDecision
{
    public required Guid VirtualMachineId { get; init; }
    public required RecoveryAction Action { get; init; }
    public required string Reason { get; init; }
    public bool Suppressed { get; init; }
    public string? SuppressionReason { get; init; }
}

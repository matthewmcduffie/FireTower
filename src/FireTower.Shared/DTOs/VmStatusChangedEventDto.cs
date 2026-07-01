using FireTower.Shared.Enums;

namespace FireTower.Shared.DTOs;

/// <summary>
/// Payload of the server-to-client "VM Status Changed" notification described in ipc.md.
/// </summary>
public sealed class VmStatusChangedEventDto
{
    public required Guid VirtualMachineId { get; init; }
    public required VmPowerState PowerState { get; init; }
    public required HealthState Health { get; init; }
    public required RecoveryState RecoveryState { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
}

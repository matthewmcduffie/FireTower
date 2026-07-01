using FireTower.Shared.Enums;

namespace FireTower.Shared.DTOs;

/// <summary>
/// A monitored virtual machine as seen by the tray application, returned over IPC.
/// </summary>
public sealed record VirtualMachineDto
{
    public required Guid Id { get; init; }
    public required string ExternalId { get; init; }
    public required string Name { get; init; }
    public required string ProviderId { get; init; }
    public required bool Enabled { get; init; }
    public required VmPowerState PowerState { get; init; }
    public required HealthState Health { get; init; }
    public required RecoveryState RecoveryState { get; init; }
    public required string HealthProfileId { get; init; }
    public required string RecoveryProfileId { get; init; }
    public int RestartCount { get; init; }
    public DateTimeOffset? LastHealthCheck { get; init; }
    public DateTimeOffset? LastRestart { get; init; }
    public IReadOnlyList<string> Tags { get; init; } = Array.Empty<string>();
}

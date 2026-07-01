using FireTower.Shared.Enums;

namespace FireTower.Core.Models;

/// <summary>
/// FireTower's internal record of a monitored virtual machine. Combines configuration
/// (identity, provider, profiles) with the latest known runtime state.
/// </summary>
public sealed class VirtualMachine
{
    public required Guid Id { get; init; }
    public required string ExternalId { get; init; }
    public required string Name { get; init; }
    public required string ProviderId { get; init; }
    public required string HealthProfileId { get; init; }
    public required string RecoveryProfileId { get; init; }
    public bool Enabled { get; set; } = true;
    public IReadOnlyList<string> Tags { get; init; } = Array.Empty<string>();
    public string? Notes { get; init; }

    public VmPowerState PowerState { get; set; } = VmPowerState.Unknown;
    public HealthState Health { get; set; } = HealthState.Unknown;
    public RecoveryState RecoveryState { get; set; } = RecoveryState.Idle;
    public int RestartCount { get; set; }
    public DateTimeOffset? LastHealthCheck { get; set; }
    public DateTimeOffset? LastRestart { get; set; }

    public DateTimeOffset DateCreated { get; init; }
    public DateTimeOffset DateModified { get; set; }
}

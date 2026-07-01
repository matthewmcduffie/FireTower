using FireTower.Shared.Enums;

namespace FireTower.Shared.DTOs;

/// <summary>
/// A virtual machine found by provider discovery but not yet imported into monitoring.
/// </summary>
public sealed class DiscoveredVirtualMachineDto
{
    public required string ExternalId { get; init; }
    public required string Name { get; init; }
    public required string ProviderId { get; init; }
    public required VmPowerState PowerState { get; init; }
    public string? OperatingSystem { get; init; }
    public string? ConfigurationPath { get; init; }
    public bool HasSnapshots { get; init; }
    public required DateTimeOffset DiscoveredAt { get; init; }
    public bool AlreadyMonitored { get; init; }
}

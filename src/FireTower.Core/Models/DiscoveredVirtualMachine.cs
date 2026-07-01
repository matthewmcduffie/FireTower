using FireTower.Shared.Enums;

namespace FireTower.Core.Models;

/// <summary>
/// A virtual machine found by provider discovery, before any decision is made about
/// whether to monitor it.
/// </summary>
public sealed class DiscoveredVirtualMachine
{
    public required string ExternalId { get; init; }
    public required string Name { get; init; }
    public required string ProviderId { get; init; }
    public required VmPowerState PowerState { get; init; }
    public string? OperatingSystem { get; init; }
    public string? ConfigurationPath { get; init; }
    public bool HasSnapshots { get; init; }
    public required DateTimeOffset DiscoveredAt { get; init; }
}

using FireTower.Shared.Enums;

namespace FireTower.Core.Models;

/// <summary>
/// Point-in-time runtime information returned by a provider for a single virtual machine.
/// </summary>
public sealed class VmRuntimeInfo
{
    public required string ExternalId { get; init; }
    public required VmPowerState PowerState { get; init; }
    public string? OperatingSystem { get; init; }
    public string? GuestAdditionsVersion { get; init; }
    public TimeSpan? GuestUptime { get; init; }
    public long? MemoryAllocationMb { get; init; }
    public int? CpuAllocation { get; init; }
    public bool HasSnapshots { get; init; }
    public required DateTimeOffset RetrievedAt { get; init; }
}

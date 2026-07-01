using FireTower.Shared.Enums;

namespace FireTower.Shared.DTOs;

/// <summary>
/// Describes a registered provider's identity, version, and advertised capabilities.
/// </summary>
public sealed class ProviderInfoDto
{
    public required string ProviderId { get; init; }
    public required string FriendlyName { get; init; }
    public required string Version { get; init; }
    public required bool IsAvailable { get; init; }
    public string? UnavailableReason { get; init; }
    public IReadOnlyList<ProviderCapability> Capabilities { get; init; } = Array.Empty<ProviderCapability>();
}

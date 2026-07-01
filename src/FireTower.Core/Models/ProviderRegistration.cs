using FireTower.Shared.Enums;

namespace FireTower.Core.Models;

/// <summary>
/// Identity and advertised capabilities of a loaded provider, as reported by
/// <see cref="Interfaces.IVmProvider.GetCapabilitiesAsync"/> during registration.
/// </summary>
public sealed class ProviderRegistration
{
    public required string ProviderId { get; init; }
    public required string FriendlyName { get; init; }
    public required string Version { get; init; }
    public bool IsAvailable { get; set; }
    public string? UnavailableReason { get; set; }
    public IReadOnlyList<ProviderCapability> Capabilities { get; set; } =
        Array.Empty<ProviderCapability>();
}

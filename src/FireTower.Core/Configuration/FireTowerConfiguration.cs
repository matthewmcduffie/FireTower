using FireTower.Core.Models;

namespace FireTower.Core.Configuration;

/// <summary>
/// The complete, in-memory representation of FireTower's configuration, assembled by the
/// Configuration Manager from firetower.json, providers.json, health-profiles.json,
/// recovery-profiles.json, and the monitored VM list. Files are split on disk for
/// readability; this is the single object the rest of the application depends on.
/// </summary>
public sealed class FireTowerConfiguration
{
    public GlobalOptions Global { get; set; } = new();
    public List<ProviderOptions> Providers { get; set; } = new();
    public List<VirtualMachineConfig> VirtualMachines { get; set; } = new();
    public List<HealthProfile> HealthProfiles { get; set; } = new();
    public List<RecoveryProfile> RecoveryProfiles { get; set; } = new();
}

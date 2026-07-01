using FireTower.Core.Models;
using FireTower.Shared.Enums;

namespace FireTower.Core.Configuration;

/// <summary>
/// Produces a minimal, working configuration so FireTower can run immediately after
/// installation without requiring every setting to be specified up front.
/// </summary>
public static class ConfigurationDefaults
{
    public const string DefaultHealthProfileId = "default-health";
    public const string DefaultRecoveryProfileId = "default-recovery";

    public static FireTowerConfiguration Create()
    {
        return new FireTowerConfiguration
        {
            Global = new GlobalOptions(),
            // Include the VirtualBox provider by default so Discovery can immediately
            // add VMs without the validator rejecting them for referencing an "unknown
            // provider". The service tolerates providers that fail to initialize (e.g.
            // VirtualBox not installed), so this entry is safe on all machines.
            Providers = new List<ProviderOptions>
            {
                new() { ProviderId = "virtualbox", Enabled = true },
            },
            VirtualMachines = new List<VirtualMachineConfig>(),
            HealthProfiles = new List<HealthProfile> { CreateDefaultHealthProfile() },
            RecoveryProfiles = new List<RecoveryProfile> { CreateDefaultRecoveryProfile() },
        };
    }

    private static HealthProfile CreateDefaultHealthProfile() => new()
    {
        Id = DefaultHealthProfileId,
        Name = "Default",
        Checks = new[]
        {
            new HealthCheckDefinition
            {
                Id = "provider-status",
                Kind = HealthCheckKind.ProviderStatus,
                IntervalSeconds = 10,
                TimeoutSeconds = 8,
                RetryCount = 1,
                FailureThreshold = 1,
                RecoveryThreshold = 1,
            },
        },
    };

    private static RecoveryProfile CreateDefaultRecoveryProfile() => new()
    {
        Id = DefaultRecoveryProfileId,
        Name = "Default",
        PreferGracefulRestart = true,

        // Wait 30 s between restart attempts to avoid hammering a VM that keeps
        // crashing immediately (e.g. bad startup script).
        CooldownSeconds = 30,

        // Never give up — the VM must always be running.
        // int.MaxValue means the window check never triggers the "retry limit exceeded"
        // suppression path.
        MaxRestartAttempts = int.MaxValue,
        RetryWindowSeconds = 3600,

        // Try a graceful restart first; if the VM is in a bad state, escalate to
        // a force restart. Never stop trying — there is no Notify-only terminal step.
        EscalationSequence = new[] { RecoveryAction.Restart, RecoveryAction.ForceRestart },
    };
}

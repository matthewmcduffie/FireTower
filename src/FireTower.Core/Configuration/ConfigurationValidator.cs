namespace FireTower.Core.Configuration;

/// <summary>
/// Validates a <see cref="FireTowerConfiguration"/> before it is saved or applied.
/// Kept independent of file I/O so it can be unit tested without touching disk.
/// </summary>
public static class ConfigurationValidator
{
    public static IReadOnlyList<string> Validate(FireTowerConfiguration configuration)
    {
        var errors = new List<string>();

        if (configuration.Global.ConfigurationVersion < 1)
        {
            errors.Add("Global configuration version must be at least 1.");
        }

        if (configuration.Global.DefaultPollingIntervalSeconds <= 0)
        {
            errors.Add("Default polling interval must be greater than zero.");
        }

        var healthProfileIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var profile in configuration.HealthProfiles)
        {
            if (!healthProfileIds.Add(profile.Id))
            {
                errors.Add($"Duplicate Health Profile id '{profile.Id}'.");
            }
        }

        var recoveryProfileIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var profile in configuration.RecoveryProfiles)
        {
            if (!recoveryProfileIds.Add(profile.Id))
            {
                errors.Add($"Duplicate Recovery Profile id '{profile.Id}'.");
            }

            if (profile.MaxRestartAttempts <= 0)
            {
                errors.Add($"Recovery Profile '{profile.Id}' must allow at least one restart attempt.");
            }

            if (profile.CooldownSeconds < 0)
            {
                errors.Add($"Recovery Profile '{profile.Id}' cooldown cannot be negative.");
            }
        }

        var providerIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var provider in configuration.Providers)
        {
            if (!providerIds.Add(provider.ProviderId))
            {
                errors.Add($"Duplicate provider id '{provider.ProviderId}'.");
            }
        }

        var vmIds = new HashSet<Guid>();
        var vmNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var vm in configuration.VirtualMachines)
        {
            if (!vmIds.Add(vm.Id))
            {
                errors.Add($"Duplicate virtual machine id '{vm.Id}'.");
            }

            if (!vmNames.Add(vm.Name))
            {
                errors.Add($"Duplicate virtual machine name '{vm.Name}'.");
            }

            if (!healthProfileIds.Contains(vm.HealthProfileId))
            {
                errors.Add($"Virtual machine '{vm.Name}' references unknown Health Profile '{vm.HealthProfileId}'.");
            }

            if (!recoveryProfileIds.Contains(vm.RecoveryProfileId))
            {
                errors.Add($"Virtual machine '{vm.Name}' references unknown Recovery Profile '{vm.RecoveryProfileId}'.");
            }

            if (!providerIds.Contains(vm.ProviderId))
            {
                errors.Add($"Virtual machine '{vm.Name}' references unknown provider '{vm.ProviderId}'.");
            }
        }

        return errors;
    }
}

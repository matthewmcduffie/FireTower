using FireTower.Core.Configuration;
using FireTower.Core.Models;
using FireTower.Shared.Enums;

namespace FireTower.Tests.Configuration;

/// <summary>
/// Unit tests for <see cref="ConfigurationValidator.Validate"/>, per testing.md's
/// Configuration Testing requirements. No disk I/O; validator is a pure function.
/// </summary>
public sealed class ConfigurationValidatorTests
{
    private static FireTowerConfiguration ValidConfiguration() =>
        new()
        {
            Global = new GlobalOptions { DefaultPollingIntervalSeconds = 30 },
            Providers = new List<ProviderOptions> { new() { ProviderId = "virtualbox" } },
            HealthProfiles = new List<HealthProfile> { new() { Id = "hp1", Name = "Default" } },
            RecoveryProfiles = new List<RecoveryProfile> { new() { Id = "rp1", Name = "Default", MaxRestartAttempts = 3 } },
            VirtualMachines = new List<VirtualMachineConfig>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    ExternalId = "vm-001",
                    Name = "TestVM",
                    ProviderId = "virtualbox",
                    HealthProfileId = "hp1",
                    RecoveryProfileId = "rp1",
                },
            },
        };

    [Fact]
    public void Validate_ReturnsNoErrors_ForValidConfiguration()
    {
        var errors = ConfigurationValidator.Validate(ValidConfiguration());
        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_ReturnsError_WhenPollingIntervalIsZero()
    {
        var config = ValidConfiguration();
        config.Global.DefaultPollingIntervalSeconds = 0;
        var errors = ConfigurationValidator.Validate(config);
        Assert.NotEmpty(errors);
    }

    [Fact]
    public void Validate_ReturnsError_WhenDuplicateHealthProfileId()
    {
        var config = ValidConfiguration();
        config.HealthProfiles.Add(new HealthProfile { Id = "hp1", Name = "Duplicate" });
        var errors = ConfigurationValidator.Validate(config);
        Assert.Contains(errors, e => e.Contains("hp1"));
    }

    [Fact]
    public void Validate_ReturnsError_WhenDuplicateRecoveryProfileId()
    {
        var config = ValidConfiguration();
        config.RecoveryProfiles.Add(new RecoveryProfile { Id = "rp1", Name = "Duplicate", MaxRestartAttempts = 1 });
        var errors = ConfigurationValidator.Validate(config);
        Assert.Contains(errors, e => e.Contains("rp1"));
    }

    [Fact]
    public void Validate_ReturnsError_WhenVmReferencesUnknownHealthProfile()
    {
        var config = ValidConfiguration();
        config.VirtualMachines.First().HealthProfileId = "nonexistent";
        var errors = ConfigurationValidator.Validate(config);
        Assert.Contains(errors, e => e.Contains("nonexistent"));
    }

    [Fact]
    public void Validate_ReturnsError_WhenVmReferencesUnknownRecoveryProfile()
    {
        var config = ValidConfiguration();
        config.VirtualMachines.First().RecoveryProfileId = "nonexistent";
        var errors = ConfigurationValidator.Validate(config);
        Assert.Contains(errors, e => e.Contains("nonexistent"));
    }

    [Fact]
    public void Validate_ReturnsError_WhenVmReferencesUnknownProvider()
    {
        var config = ValidConfiguration();
        config.VirtualMachines.First().ProviderId = "hyper-v";
        var errors = ConfigurationValidator.Validate(config);
        Assert.Contains(errors, e => e.Contains("hyper-v"));
    }

    [Fact]
    public void Validate_ReturnsError_WhenDuplicateVmNames()
    {
        var config = ValidConfiguration();
        config.VirtualMachines.Add(new VirtualMachineConfig
        {
            Id = Guid.NewGuid(),
            ExternalId = "vm-002",
            Name = "TestVM",
            ProviderId = "virtualbox",
            HealthProfileId = "hp1",
            RecoveryProfileId = "rp1",
        });
        var errors = ConfigurationValidator.Validate(config);
        Assert.Contains(errors, e => e.Contains("TestVM"));
    }

    [Fact]
    public void Validate_ReturnsError_WhenMaxRestartAttemptsIsZero()
    {
        var config = ValidConfiguration();
        config.RecoveryProfiles[0] = new RecoveryProfile { Id = "rp1", Name = "Bad", MaxRestartAttempts = 0 };
        var errors = ConfigurationValidator.Validate(config);
        Assert.NotEmpty(errors);
    }
}

using FireTower.Core.Models;
using FireTower.Shared.DTOs;
using FireTower.Shared.Enums;

namespace FireTower.Service.Ipc;

/// <summary>
/// Maps Core domain models to the Shared DTOs returned across the IPC boundary, keeping
/// that translation in one place rather than scattered through every handler method.
/// </summary>
internal static class DtoMapping
{
    public static VirtualMachineDto ToDto(VirtualMachine vm) => new()
    {
        Id = vm.Id,
        ExternalId = vm.ExternalId,
        Name = vm.Name,
        ProviderId = vm.ProviderId,
        Enabled = vm.Enabled,
        PowerState = vm.PowerState,
        Health = vm.Health,
        RecoveryState = vm.RecoveryState,
        HealthProfileId = vm.HealthProfileId,
        RecoveryProfileId = vm.RecoveryProfileId,
        RestartCount = vm.RestartCount,
        LastHealthCheck = vm.LastHealthCheck,
        LastRestart = vm.LastRestart,
        Tags = vm.Tags,
    };

    public static ProviderInfoDto ToDto(ProviderRegistration registration) => new()
    {
        ProviderId = registration.ProviderId,
        FriendlyName = registration.FriendlyName,
        Version = registration.Version,
        IsAvailable = registration.IsAvailable,
        UnavailableReason = registration.UnavailableReason,
        Capabilities = registration.Capabilities,
    };

    public static DiscoveredVirtualMachineDto ToDto(DiscoveredVirtualMachine discovered, bool alreadyMonitored) => new()
    {
        ExternalId = discovered.ExternalId,
        Name = discovered.Name,
        ProviderId = discovered.ProviderId,
        PowerState = discovered.PowerState,
        OperatingSystem = discovered.OperatingSystem,
        ConfigurationPath = discovered.ConfigurationPath,
        HasSnapshots = discovered.HasSnapshots,
        DiscoveredAt = discovered.DiscoveredAt,
        AlreadyMonitored = alreadyMonitored,
    };

    public static EventRecordDto ToDto(OperationalEvent operationalEvent) => new()
    {
        Timestamp = operationalEvent.Timestamp,
        Category = operationalEvent.Category,
        Message = operationalEvent.Message,
        VirtualMachine = operationalEvent.VirtualMachineId?.ToString(),
        CorrelationId = operationalEvent.CorrelationId,
    };

    public static StatisticsSnapshotDto ToDto(StatisticsSnapshot snapshot, TimeSpan uptime) => new()
    {
        Timestamp = snapshot.Timestamp,
        TotalVmCount = snapshot.TotalVmCount,
        HealthyCount = snapshot.HealthyCount,
        WarningCount = snapshot.WarningCount,
        CriticalCount = snapshot.CriticalCount,
        RestartCount = snapshot.RestartCount,
        AverageRestartDurationSeconds = snapshot.AverageRestartDurationSeconds,
        AverageHealthCheckDurationMs = snapshot.AverageHealthCheckDurationMs,
        ServiceUptime = uptime,
    };

    public static VmStatusChangedEventDto ToStatusChangedDto(VirtualMachine vm) => new()
    {
        VirtualMachineId = vm.Id,
        PowerState = vm.PowerState,
        Health = vm.Health,
        RecoveryState = vm.RecoveryState,
        Timestamp = DateTimeOffset.UtcNow,
    };
}

using FireTower.Core.Models;

namespace FireTower.Core.Interfaces;

/// <summary>
/// Publishes important operational events (health changes, restarts, configuration reloads)
/// for consumption by the IPC layer and the database's event history. Core raises events
/// through this interface without knowing who, if anyone, is listening.
/// </summary>
public interface IEventPublisher
{
    void PublishVmStatusChanged(VirtualMachine virtualMachine);

    void PublishConfigurationReloaded();

    void PublishProviderStatusChanged(ProviderRegistration provider);

    void PublishOperationalEvent(string category, string message, Guid? virtualMachineId, string? correlationId);
}

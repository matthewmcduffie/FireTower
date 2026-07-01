using FireTower.Core.Models;
using FireTower.Shared.Enums;

namespace FireTower.Core.Interfaces;

/// <summary>
/// The common interface every virtualization provider must implement, per providers.md.
/// FireTower.Core never references a concrete provider type; it only ever depends on this
/// interface, resolved through the Provider Manager.
/// </summary>
public interface IVmProvider
{
    string ProviderId { get; }
    string FriendlyName { get; }

    Task InitializeAsync(CancellationToken cancellationToken);

    Task ShutdownAsync(CancellationToken cancellationToken);

    Task<ProviderRegistration> GetCapabilitiesAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<DiscoveredVirtualMachine>> DiscoverVirtualMachinesAsync(CancellationToken cancellationToken);

    Task<VmRuntimeInfo> GetRuntimeInfoAsync(string externalId, CancellationToken cancellationToken);

    Task<VmPowerState> GetStateAsync(string externalId, CancellationToken cancellationToken);

    Task StartAsync(string externalId, CancellationToken cancellationToken);

    Task StopAsync(string externalId, CancellationToken cancellationToken);

    Task PowerOffAsync(string externalId, CancellationToken cancellationToken);

    Task GracefulShutdownAsync(string externalId, CancellationToken cancellationToken);

    Task RestartAsync(string externalId, CancellationToken cancellationToken);

    Task ForceRestartAsync(string externalId, CancellationToken cancellationToken);
}

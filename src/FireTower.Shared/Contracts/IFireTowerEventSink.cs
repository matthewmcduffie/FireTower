using FireTower.Shared.DTOs;

namespace FireTower.Shared.Contracts;

/// <summary>
/// Notifications the service pushes to a connected client, matching the Events catalog in
/// ipc.md. The tray application registers an implementation of this interface as a local
/// StreamJsonRpc target so the service can call these methods by name via
/// <c>JsonRpc.NotifyAsync</c> without the client polling for changes.
/// </summary>
public interface IFireTowerEventSink
{
    Task OnVmStatusChangedAsync(VmStatusChangedEventDto payload);

    Task OnConfigurationReloadedAsync();

    Task OnServiceStatusChangedAsync(ServiceStatusDto status);

    Task OnProviderStatusChangedAsync(ProviderInfoDto provider);
}

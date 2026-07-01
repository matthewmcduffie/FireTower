using FireTower.Shared.Contracts;
using FireTower.Shared.DTOs;
using FireTower.Shared.Enums;

namespace FireTower.Tray.Services.Ipc;

/// <summary>
/// The tray application's view of its connection to the FireTower Windows Service.
/// ViewModels depend on this interface rather than StreamJsonRpc directly, per tray.md's
/// requirement that the UI never contain business logic or transport details.
/// </summary>
public interface IFireTowerIpcClient
{
    ConnectionState State { get; }

    IFireTowerService Service { get; }

    event EventHandler<ConnectionState>? ConnectionStateChanged;
    event EventHandler<VmStatusChangedEventDto>? VmStatusChanged;
    event EventHandler? ConfigurationReloaded;
    event EventHandler<ServiceStatusDto>? ServiceStatusChanged;
    event EventHandler<ProviderInfoDto>? ProviderStatusChanged;

    void Start();

    Task StopAsync();
}

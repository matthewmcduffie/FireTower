using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FireTower.Shared.DTOs;
using FireTower.Tray.Services.Ipc;

namespace FireTower.Tray.ViewModels;

/// <summary>
/// About page: application, provider, and database version information, per tray.md.
/// </summary>
public sealed partial class AboutViewModel : ViewModelBase
{
    private readonly IFireTowerIpcClient _ipcClient;

    [ObservableProperty]
    private ServiceStatusDto? _serviceStatus;

    public string TrayVersion => Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.0.0";

    public AboutViewModel(IFireTowerIpcClient ipcClient)
    {
        _ipcClient = ipcClient;
        _ = RefreshCommand.ExecuteAsync(null);
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        if (_ipcClient.State != Shared.Enums.ConnectionState.Connected)
        {
            return;
        }

        var result = await _ipcClient.Service.GetServiceStatusAsync(CancellationToken.None);
        ServiceStatus = result.Payload;
    }
}

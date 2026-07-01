using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FireTower.Shared.DTOs;
using FireTower.Tray.Services;
using FireTower.Tray.Services.Ipc;

namespace FireTower.Tray.ViewModels;

/// <summary>
/// Dashboard page: a high-level summary, per the Dashboard section of tray.md and
/// ui-guidelines.md. Never more than a summary — details live on their own pages.
/// </summary>
public sealed partial class DashboardViewModel : ViewModelBase
{
    private readonly IFireTowerIpcClient _ipcClient;
    private readonly IUiDispatcher _dispatcher;

    [ObservableProperty]
    private ServiceStatusDto? _serviceStatus;

    public ObservableCollection<EventRecordDto> RecentEvents { get; } = new();

    public DashboardViewModel(IFireTowerIpcClient ipcClient, IUiDispatcher dispatcher)
    {
        _ipcClient = ipcClient;
        _dispatcher = dispatcher;
        _ipcClient.ServiceStatusChanged += (_, status) => _dispatcher.Invoke(() => ServiceStatus = status);
        _ = RefreshCommand.ExecuteAsync(null);
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        if (_ipcClient.State != Shared.Enums.ConnectionState.Connected)
        {
            return;
        }

        IsBusy = true;
        try
        {
            var statusResult = await _ipcClient.Service.GetServiceStatusAsync(CancellationToken.None);

            if (!statusResult.Success)
            {
                ShowError($"Could not retrieve service status: {statusResult.ErrorMessage}");
                return;
            }

            ServiceStatus = statusResult.Payload;

            var eventsResult = await _ipcClient.Service.GetEventsAsync(0, 20, CancellationToken.None);
            RecentEvents.Clear();
            if (eventsResult.Success && eventsResult.Payload is not null)
            {
                foreach (var entry in eventsResult.Payload)
                {
                    RecentEvents.Add(entry);
                }
            }
        }
        catch (Exception ex)
        {
            ShowError($"Dashboard refresh failed: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }
}

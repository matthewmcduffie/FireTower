using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using FireTower.Shared.DTOs;
using FireTower.Tray.Services.Ipc;

namespace FireTower.Tray.ViewModels;

/// <summary>
/// Events page: important operational events, per tray.md.
/// </summary>
public sealed partial class EventsViewModel : ViewModelBase
{
    private readonly IFireTowerIpcClient _ipcClient;

    public ObservableCollection<EventRecordDto> Events { get; } = new();

    public EventsViewModel(IFireTowerIpcClient ipcClient)
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

        IsBusy = true;
        try
        {
            var result = await _ipcClient.Service.GetEventsAsync(0, 200, CancellationToken.None);
            ErrorMessage = result.Success ? null : result.ErrorMessage;

            Events.Clear();
            if (result.Success && result.Payload is not null)
            {
                foreach (var entry in result.Payload)
                {
                    Events.Add(entry);
                }
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }
}

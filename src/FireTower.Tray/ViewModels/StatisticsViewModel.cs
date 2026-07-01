using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FireTower.Shared.DTOs;
using FireTower.Tray.Services.Ipc;

namespace FireTower.Tray.ViewModels;

/// <summary>
/// Statistics page: historical fleet-wide statistics, per tray.md.
/// </summary>
public sealed partial class StatisticsViewModel : ViewModelBase
{
    private readonly IFireTowerIpcClient _ipcClient;

    [ObservableProperty]
    private StatisticsSnapshotDto? _statistics;

    public StatisticsViewModel(IFireTowerIpcClient ipcClient)
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
            var result = await _ipcClient.Service.GetStatisticsAsync(CancellationToken.None);
            Statistics = result.Payload;
            ErrorMessage = result.Success ? null : result.ErrorMessage;
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

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FireTower.Shared.DTOs;
using FireTower.Tray.Services.Ipc;

namespace FireTower.Tray.ViewModels;

/// <summary>
/// Logs page: rolling application logs with filtering and search, per tray.md and
/// logging.md. Logs are never editable here.
/// </summary>
public sealed partial class LogsViewModel : ViewModelBase
{
    private readonly IFireTowerIpcClient _ipcClient;

    [ObservableProperty]
    private string? _searchText;

    [ObservableProperty]
    private string? _componentFilter;

    public ObservableCollection<LogEntryDto> Logs { get; } = new();

    public LogsViewModel(IFireTowerIpcClient ipcClient)
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
            var query = new LogQueryDto
            {
                SearchText = string.IsNullOrWhiteSpace(SearchText) ? null : SearchText,
                Component = string.IsNullOrWhiteSpace(ComponentFilter) ? null : ComponentFilter,
                Take = 500,
            };

            var result = await _ipcClient.Service.GetLogsAsync(query, CancellationToken.None);
            ErrorMessage = result.Success ? null : result.ErrorMessage;

            Logs.Clear();
            if (result.Success && result.Payload is not null)
            {
                foreach (var entry in result.Payload)
                {
                    Logs.Add(entry);
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

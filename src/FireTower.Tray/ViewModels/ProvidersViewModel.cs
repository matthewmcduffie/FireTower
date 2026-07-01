using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using FireTower.Shared.DTOs;
using FireTower.Tray.Services.Ipc;

namespace FireTower.Tray.ViewModels;

/// <summary>
/// Providers page: installed providers and their capabilities, per tray.md.
/// </summary>
public sealed partial class ProvidersViewModel : ViewModelBase
{
    private readonly IFireTowerIpcClient _ipcClient;

    public ObservableCollection<ProviderInfoDto> Providers { get; } = new();

    public ProvidersViewModel(IFireTowerIpcClient ipcClient)
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
            var result = await _ipcClient.Service.GetProvidersAsync(CancellationToken.None);
            ErrorMessage = result.Success ? null : result.ErrorMessage;

            Providers.Clear();
            if (result.Success && result.Payload is not null)
            {
                foreach (var provider in result.Payload)
                {
                    Providers.Add(provider);
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

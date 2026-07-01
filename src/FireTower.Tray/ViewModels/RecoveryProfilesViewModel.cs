using System.Collections.ObjectModel;
using System.Text.Json;
using CommunityToolkit.Mvvm.Input;
using FireTower.Core.Configuration;
using FireTower.Core.Models;
using FireTower.Tray.Services.Ipc;

namespace FireTower.Tray.ViewModels;

/// <summary>
/// Recovery Profiles page: configured Recovery Profiles, per tray.md. Read-only here; see
/// the note on <see cref="HealthProfilesViewModel"/> regarding editing.
/// </summary>
public sealed partial class RecoveryProfilesViewModel : ViewModelBase
{
    private readonly IFireTowerIpcClient _ipcClient;

    public ObservableCollection<RecoveryProfile> Profiles { get; } = new();

    public RecoveryProfilesViewModel(IFireTowerIpcClient ipcClient)
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
            var result = await _ipcClient.Service.GetConfigurationAsync(CancellationToken.None);
            ErrorMessage = result.Success ? null : result.ErrorMessage;

            Profiles.Clear();
            if (result.Success && result.Payload is not null)
            {
                var configuration = JsonSerializer.Deserialize<FireTowerConfiguration>(result.Payload, ConfigurationSerialization.Options);
                foreach (var profile in configuration?.RecoveryProfiles ?? new List<RecoveryProfile>())
                {
                    Profiles.Add(profile);
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

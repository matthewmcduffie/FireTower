using System.Collections.ObjectModel;
using System.Text.Json;
using CommunityToolkit.Mvvm.Input;
using FireTower.Core.Configuration;
using FireTower.Core.Models;
using FireTower.Tray.Services.Ipc;

namespace FireTower.Tray.ViewModels;

/// <summary>
/// Health Checks page: configured Health Profiles, per tray.md. Profiles are read here;
/// editing the underlying JSON happens on the Settings page (configuration.md explicitly
/// keeps manual JSON editing as a supported path alongside any future structured editor).
/// </summary>
public sealed partial class HealthProfilesViewModel : ViewModelBase
{
    private readonly IFireTowerIpcClient _ipcClient;

    public ObservableCollection<HealthProfile> Profiles { get; } = new();

    public HealthProfilesViewModel(IFireTowerIpcClient ipcClient)
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
                foreach (var profile in configuration?.HealthProfiles ?? new List<HealthProfile>())
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

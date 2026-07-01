using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FireTower.Tray.Services;
using FireTower.Tray.Services.Ipc;

namespace FireTower.Tray.ViewModels;

/// <summary>
/// Settings page: service configuration (edited as JSON, per configuration.md's explicit
/// support for manual editing) plus local tray preferences, per tray.md.
/// </summary>
public sealed partial class SettingsViewModel : ViewModelBase
{
    private readonly IFireTowerIpcClient _ipcClient;
    private readonly ITrayPreferencesService _preferences;

    [ObservableProperty]
    private string _configurationJson = string.Empty;

    [ObservableProperty]
    private string? _statusMessage;

    [ObservableProperty]
    private bool _launchAtLogin;

    [ObservableProperty]
    private string _theme = "Dark";

    public SettingsViewModel(IFireTowerIpcClient ipcClient, ITrayPreferencesService preferences)
    {
        _ipcClient = ipcClient;
        _preferences = preferences;
        LaunchAtLogin = preferences.Current.LaunchAtLogin;
        Theme = preferences.Current.Theme;
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
            if (result.Success)
            {
                ConfigurationJson = result.Payload ?? string.Empty;
                ErrorMessage = null;
            }
            else
            {
                ErrorMessage = result.ErrorMessage;
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

    [RelayCommand]
    private async Task SaveConfigurationAsync()
    {
        IsBusy = true;
        try
        {
            var result = await _ipcClient.Service.SaveConfigurationAsync(ConfigurationJson, CancellationToken.None);
            StatusMessage = result.Success ? "Configuration saved." : null;
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

    [RelayCommand]
    private async Task ReloadConfigurationAsync()
    {
        IsBusy = true;
        try
        {
            var result = await _ipcClient.Service.ReloadConfigurationAsync(CancellationToken.None);
            StatusMessage = result.Success ? "Configuration reloaded." : null;
            ErrorMessage = result.Success ? null : result.ErrorMessage;
            if (result.Success)
            {
                await RefreshAsync();
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

    [RelayCommand]
    private async Task PauseMonitoringAsync() =>
        await ToggleMonitoringAsync(() => _ipcClient.Service.PauseMonitoringAsync(CancellationToken.None));

    [RelayCommand]
    private async Task ResumeMonitoringAsync() =>
        await ToggleMonitoringAsync(() => _ipcClient.Service.ResumeMonitoringAsync(CancellationToken.None));

    private async Task ToggleMonitoringAsync(Func<Task<FireTower.Shared.Contracts.OperationResult<FireTower.Shared.Contracts.Unit>>> operation)
    {
        IsBusy = true;
        try
        {
            var result = await operation();
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

    [RelayCommand]
    private async Task SaveTrayPreferencesAsync()
    {
        var preferences = _preferences.Current;
        preferences.LaunchAtLogin = LaunchAtLogin;
        preferences.Theme = Theme;
        await _preferences.SaveAsync(preferences);

        // Apply the startup preference immediately so the user doesn't need to
        // relaunch the app for the change to take effect.
        App.ApplyLaunchAtLogin(preferences.LaunchAtLogin);

        StatusMessage = "Preferences saved.";
    }
}

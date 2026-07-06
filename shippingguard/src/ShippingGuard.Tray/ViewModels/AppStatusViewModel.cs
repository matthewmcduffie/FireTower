using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShippingGuard.Core.Models;
using ShippingGuard.Tray.Ipc;

namespace ShippingGuard.Tray.ViewModels;

public sealed partial class AppStatusViewModel : ObservableObject
{
    private readonly AgentIpcClient _ipc;

    [ObservableProperty] private string _profileId = string.Empty;
    [ObservableProperty] private string _displayName = string.Empty;
    [ObservableProperty] private AppHealthState _health = AppHealthState.Unknown;
    [ObservableProperty] private bool _isRunning;
    [ObservableProperty] private bool _maintenanceMode;
    [ObservableProperty] private int _restartCount;
    [ObservableProperty] private string? _lastAction;
    [ObservableProperty] private string? _lastError;

    public string HealthLabel => Health switch
    {
        AppHealthState.Running        => "Running",
        AppHealthState.Hung           => "Hung",
        AppHealthState.Stopped        => "Stopped",
        AppHealthState.Restarting     => "Restarting…",
        AppHealthState.RetryLimitReached => "Retry Limit Reached",
        AppHealthState.MaintenanceMode   => "Maintenance",
        AppHealthState.Disabled       => "Disabled",
        _                             => "Unknown",
    };

    public string HealthColor => Health switch
    {
        AppHealthState.Running        => "#3DDC84",
        AppHealthState.Hung           => "#FFB74D",
        AppHealthState.Restarting     => "#4D8EFF",
        AppHealthState.MaintenanceMode => "#4D8EFF",
        AppHealthState.RetryLimitReached => "#FF6B6B",
        AppHealthState.Stopped        => "#FF6B6B",
        _                             => "#8C909F",
    };

    public AppStatusViewModel(AgentIpcClient ipc)
    {
        _ipc = ipc;
    }

    public void UpdateFrom(AppStatus status)
    {
        ProfileId    = status.ProfileId;
        DisplayName  = status.DisplayName;
        Health       = status.Health;
        IsRunning    = status.IsRunning;
        MaintenanceMode = status.MaintenanceMode;
        RestartCount = status.RestartCount;
        LastAction   = status.LastAction;
        LastError    = status.LastError;
        OnPropertyChanged(nameof(HealthLabel));
        OnPropertyChanged(nameof(HealthColor));
    }

    [RelayCommand] private async Task StartAsync()   => await _ipc.StartAppAsync(ProfileId);
    [RelayCommand] private async Task StopAsync()    => await _ipc.StopAppAsync(ProfileId);
    [RelayCommand] private async Task KillAsync()    => await _ipc.KillAppAsync(ProfileId);
    [RelayCommand] private async Task ToggleMaintenanceAsync() =>
        await _ipc.SetMaintenanceAsync(ProfileId, !MaintenanceMode);
}

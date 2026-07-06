using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShippingGuard.Core.Configuration;
using ShippingGuard.Core.Models;
using ShippingGuard.Tray.Ipc;
using ShippingGuard.UiAutomation;

namespace ShippingGuard.Tray.ViewModels;

public sealed partial class MainViewModel : ObservableObject, IDisposable
{
    private readonly AgentIpcClient _ipc;
    private readonly AgentSettings _settings;
    private readonly DialogWatcher _dialogWatcher;
    private readonly PeriodicTimer _pollTimer;
    private readonly Task _pollLoop;
    private readonly CancellationTokenSource _cts = new();

    [ObservableProperty] private string _agentStatus = "Connecting…";
    [ObservableProperty] private bool _isConnected;

    public ObservableCollection<AppStatusViewModel> Apps { get; } = new();

    public event EventHandler? ShowWindowRequested;
    public event EventHandler? ExitRequested;

    public MainViewModel(AgentIpcClient ipc, AgentSettings settings, DialogWatcher dialogWatcher)
    {
        _ipc = ipc;
        _settings = settings;
        _dialogWatcher = dialogWatcher;
        _pollTimer = new PeriodicTimer(TimeSpan.FromSeconds(3));
        _pollLoop = PollLoopAsync(_cts.Token);
    }

    private async Task PollLoopAsync(CancellationToken ct)
    {
        while (await _pollTimer.WaitForNextTickAsync(ct))
        {
            await RefreshAsync();
            RunDialogAutomation();
        }
    }

    private async Task RefreshAsync()
    {
        var response = await _ipc.GetStatusAsync();
        if (response?.Success != true || response.Statuses is null)
        {
            IsConnected = false;
            AgentStatus = "Agent Unavailable";
            return;
        }

        IsConnected = true;
        AgentStatus = "Connected";

        var current = response.Statuses;
        foreach (var status in current)
        {
            var vm = Apps.FirstOrDefault(a => a.ProfileId == status.ProfileId);
            if (vm is null)
            {
                vm = new AppStatusViewModel(_ipc);
                System.Windows.Application.Current.Dispatcher.Invoke(() => Apps.Add(vm));
            }
            System.Windows.Application.Current.Dispatcher.Invoke(() => vm.UpdateFrom(status));
        }

        var activeIds = current.Select(s => s.ProfileId).ToHashSet();
        foreach (var stale in Apps.Where(a => !activeIds.Contains(a.ProfileId)).ToList())
            System.Windows.Application.Current.Dispatcher.Invoke(() => Apps.Remove(stale));
    }

    private void RunDialogAutomation()
    {
        var profiles = ProfileLoader.LoadAll(_settings.ProfilesDirectory);
        foreach (var profile in profiles.Where(p => p.Enabled && p.DialogRules.Count > 0))
        {
            var vm = Apps.FirstOrDefault(a => a.ProfileId == profile.Id);
            _dialogWatcher.CheckAndHandle(profile, vm?.MaintenanceMode ?? false);
        }
    }

    [RelayCommand]
    private void ShowWindow() => ShowWindowRequested?.Invoke(this, EventArgs.Empty);

    [RelayCommand]
    private void Exit() => ExitRequested?.Invoke(this, EventArgs.Empty);

    public void Dispose()
    {
        _cts.Cancel();
        _pollTimer.Dispose();
    }
}

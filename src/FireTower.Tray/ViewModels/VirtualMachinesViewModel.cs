using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FireTower.Shared.DTOs;
using FireTower.Shared.Enums;
using FireTower.Tray.Services;
using FireTower.Tray.Services.Ipc;

namespace FireTower.Tray.ViewModels;

/// <summary>
/// Virtual Machines page: every monitored VM with manual controls, per tray.md.
/// </summary>
public sealed partial class VirtualMachinesViewModel : ViewModelBase
{
    private readonly IFireTowerIpcClient _ipcClient;
    private readonly IUiDispatcher _dispatcher;

    public ObservableCollection<VirtualMachineDto> VirtualMachines { get; } = new();

    [ObservableProperty]
    private VirtualMachineDto? _selectedVirtualMachine;

    public VirtualMachinesViewModel(IFireTowerIpcClient ipcClient, IUiDispatcher dispatcher)
    {
        _ipcClient = ipcClient;
        _dispatcher = dispatcher;
        _ipcClient.VmStatusChanged += OnVmStatusChanged;
        _ = RefreshCommand.ExecuteAsync(null);
        _ = AutoRefreshAsync();
    }

    // Poll the service every 10 seconds as a fallback so the VM list is never more
    // than 10 s stale even if an IPC push event is lost or the connection drops briefly.
    private async Task AutoRefreshAsync()
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(10));
        while (await timer.WaitForNextTickAsync())
        {
            if (!RefreshCommand.IsRunning)
            {
                await RefreshCommand.ExecuteAsync(null);
            }
        }
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        if (_ipcClient.State != ConnectionState.Connected)
        {
            return;
        }

        IsBusy = true;
        try
        {
            var result = await _ipcClient.Service.GetVirtualMachinesAsync(CancellationToken.None);

            if (!result.Success)
            {
                ShowError($"Could not load virtual machines: {result.ErrorMessage}");
                return;
            }

            VirtualMachines.Clear();
            foreach (var vm in result.Payload ?? Array.Empty<VirtualMachineDto>())
            {
                VirtualMachines.Add(vm);
            }

            if (VirtualMachines.Count == 0)
            {
                StatusMessage = "No virtual machines are configured for monitoring yet. Use Discovery to find VMs, then add them to virtual-machines.json.";
            }
            else
            {
                StatusMessage = null;
            }
        }
        catch (Exception ex)
        {
            ShowError($"Failed to load virtual machines: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private Task StartAsync(VirtualMachineDto vm) =>
        RunVmOperationAsync(vm.Name, "start", () => _ipcClient.Service.StartVirtualMachineAsync(vm.Id, CancellationToken.None));

    [RelayCommand]
    private Task StopAsync(VirtualMachineDto vm) =>
        RunVmOperationAsync(vm.Name, "stop", () => _ipcClient.Service.StopVirtualMachineAsync(vm.Id, CancellationToken.None));

    [RelayCommand]
    private Task RestartAsync(VirtualMachineDto vm) =>
        RunVmOperationAsync(vm.Name, "restart", () => _ipcClient.Service.RestartVirtualMachineAsync(vm.Id, CancellationToken.None));

    [RelayCommand]
    private Task ForceRestartAsync(VirtualMachineDto vm) =>
        RunVmOperationAsync(vm.Name, "force restart", () => _ipcClient.Service.ForceRestartVirtualMachineAsync(vm.Id, CancellationToken.None));

    [RelayCommand]
    private Task EnableMonitoringAsync(VirtualMachineDto vm) =>
        RunVmOperationAsync(vm.Name, "enable monitoring for", () => _ipcClient.Service.SetMonitoringEnabledAsync(vm.Id, true, CancellationToken.None));

    [RelayCommand]
    private Task DisableMonitoringAsync(VirtualMachineDto vm) =>
        RunVmOperationAsync(vm.Name, "disable monitoring for", () => _ipcClient.Service.SetMonitoringEnabledAsync(vm.Id, false, CancellationToken.None));

    private async Task RunVmOperationAsync(
        string vmName,
        string verb,
        Func<Task<FireTower.Shared.Contracts.OperationResult<FireTower.Shared.Contracts.Unit>>> operation)
    {
        if (_ipcClient.State != Shared.Enums.ConnectionState.Connected)
        {
            ShowError("Not connected to the FireTower service. Start the service and try again.");
            return;
        }

        IsBusy = true;
        string? operationError = null;
        bool success = false;
        try
        {
            var result = await operation();
            success = result.Success;
            if (!result.Success)
            {
                operationError = $"Could not {verb} \"{vmName}\": {result.ErrorMessage}";
            }
        }
        catch (Exception ex)
        {
            operationError = $"Could not {verb} \"{vmName}\": {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }

        // Refresh first so the VM list shows updated state, then display the outcome.
        // Showing the message after refresh ensures RefreshAsync does not overwrite it.
        await RefreshAsync();

        if (operationError is not null)
        {
            ShowError(operationError);
        }
        else if (success)
        {
            ShowStatus($"Sent \"{verb}\" to \"{vmName}\". Check the Power State column for the result.");
        }
    }

    private void OnVmStatusChanged(object? sender, VmStatusChangedEventDto payload)
    {
        _dispatcher.Invoke(() =>
        {
            var existing = VirtualMachines.FirstOrDefault(v => v.Id == payload.VirtualMachineId);
            if (existing is null) return;

            var index = VirtualMachines.IndexOf(existing);
            VirtualMachines[index] = existing with
            {
                PowerState = payload.PowerState,
                Health = payload.Health,
                RecoveryState = payload.RecoveryState,
            };
        });
    }
}

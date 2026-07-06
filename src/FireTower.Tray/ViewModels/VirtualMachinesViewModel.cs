using System.Collections.ObjectModel;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FireTower.Core.Configuration;
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
        _ = PeriodicRefreshAsync();
    }

    private async Task PeriodicRefreshAsync()
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(15));
        while (await timer.WaitForNextTickAsync())
        {
            if (!RefreshCommand.IsRunning)
                await RefreshCommand.ExecuteAsync(null);
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

            var selectedId = SelectedVirtualMachine?.Id;
            var incoming = result.Payload ?? Array.Empty<VirtualMachineDto>();
            var incomingIds = incoming.Select(v => v.Id).ToHashSet();

            // Remove VMs that no longer exist in config.
            for (int i = VirtualMachines.Count - 1; i >= 0; i--)
                if (!incomingIds.Contains(VirtualMachines[i].Id))
                    VirtualMachines.RemoveAt(i);

            // Update existing items in place; add new ones. Never Clear() — that drops selection.
            foreach (var vm in incoming)
            {
                var idx = -1;
                for (int i = 0; i < VirtualMachines.Count; i++)
                    if (VirtualMachines[i].Id == vm.Id) { idx = i; break; }
                if (idx >= 0)
                    VirtualMachines[idx] = vm;
                else
                    VirtualMachines.Add(vm);
            }

            // Replacing a record object breaks the DataGrid's selected-item reference.
            // Restore the selection by ID after any update.
            if (selectedId.HasValue)
                SelectedVirtualMachine = VirtualMachines.FirstOrDefault(v => v.Id == selectedId.Value);

            if (VirtualMachines.Count == 0)
            {
                StatusMessage = "No virtual machines configured. Go to Discovery to add one.";
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

    [RelayCommand]
    private async Task RemoveAsync(VirtualMachineDto vm)
    {
        if (_ipcClient.State != ConnectionState.Connected)
        {
            ShowError("Not connected to the FireTower service.");
            return;
        }

        IsBusy = true;
        try
        {
            var configResult = await _ipcClient.Service.GetConfigurationAsync(CancellationToken.None);
            if (!configResult.Success)
            {
                ShowError($"Could not read configuration: {configResult.ErrorMessage}");
                return;
            }

            var configuration = JsonSerializer.Deserialize<FireTowerConfiguration>(
                configResult.Payload!, ConfigurationSerialization.Options)
                ?? new FireTowerConfiguration();

            var entry = configuration.VirtualMachines.FirstOrDefault(v =>
                string.Equals(v.ExternalId, vm.ExternalId, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(v.ProviderId, vm.ProviderId, StringComparison.OrdinalIgnoreCase));

            if (entry is null) { ShowError($"\"{vm.Name}\" was not found in configuration."); return; }

            configuration.VirtualMachines.Remove(entry);

            var json = JsonSerializer.Serialize(configuration, ConfigurationSerialization.Options);
            var saveResult = await _ipcClient.Service.SaveConfigurationAsync(json, CancellationToken.None);
            if (!saveResult.Success) { ShowError($"Could not save: {saveResult.ErrorMessage}"); return; }

            await _ipcClient.Service.ReloadConfigurationAsync(CancellationToken.None);
            SelectedVirtualMachine = null;
            ShowStatus($"Removed \"{vm.Name}\" from monitoring.");
        }
        catch (Exception ex)
        {
            ShowError($"Could not remove \"{vm.Name}\": {ex.Message}");
        }
        finally
        {
            IsBusy = false;
            await RefreshAsync();
        }
    }

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

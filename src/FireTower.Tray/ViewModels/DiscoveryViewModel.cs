using System.Collections.ObjectModel;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FireTower.Core.Configuration;
using FireTower.Tray.Services.Ipc;

namespace FireTower.Tray.ViewModels;

/// <summary>
/// Discovery page: finds VMs via provider discovery, lets the user select which ones to
/// add to monitoring, then writes them into configuration so the service starts watching
/// them immediately — all without the user ever editing a JSON file, per tray.md.
/// </summary>
public sealed partial class DiscoveryViewModel : ViewModelBase
{
    private readonly IFireTowerIpcClient _ipcClient;

    public ObservableCollection<SelectableDiscoveredVm> Discovered { get; } = new();

    [ObservableProperty]
    private bool _hasUnmonitoredSelection;

    public DiscoveryViewModel(IFireTowerIpcClient ipcClient)
    {
        _ipcClient = ipcClient;
        _ = RunDiscoveryCommand.ExecuteAsync(null);
    }

    [RelayCommand]
    private async Task RunDiscoveryAsync()
    {
        if (_ipcClient.State != Shared.Enums.ConnectionState.Connected)
        {
            ShowError("Not connected to the FireTower service. Start the service and try again.");
            return;
        }

        IsBusy = true;
        try
        {
            var result = await _ipcClient.Service.DiscoverVirtualMachinesAsync(null, CancellationToken.None);

            if (!result.Success)
            {
                ShowError($"Discovery failed: {result.ErrorMessage}");
                return;
            }

            Discovered.Clear();
            foreach (var vm in result.Payload ?? Array.Empty<Shared.DTOs.DiscoveredVirtualMachineDto>())
            {
                var selectable = new SelectableDiscoveredVm
                {
                    Vm = vm,
                    IsSelected = !vm.AlreadyMonitored,
                };
                selectable.PropertyChanged += (_, _) => RefreshSelectionState();
                Discovered.Add(selectable);
            }

            RefreshSelectionState();

            ShowStatus(Discovered.Count == 0
                ? "No virtual machines found. Is VirtualBox running? Are any VMs defined?"
                : $"Found {Discovered.Count} VM(s). {Discovered.Count(v => !v.Vm.AlreadyMonitored)} not yet monitored.");
        }
        catch (Exception ex)
        {
            ShowError($"Discovery failed: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void SelectAll()
    {
        foreach (var item in Discovered.Where(v => !v.Vm.AlreadyMonitored))
        {
            item.IsSelected = true;
        }
    }

    [RelayCommand]
    private void SelectNone()
    {
        foreach (var item in Discovered)
        {
            item.IsSelected = false;
        }
    }

    [RelayCommand]
    private async Task AddSelectedToMonitoringAsync()
    {
        var toAdd = Discovered.Where(v => v.IsSelected && !v.Vm.AlreadyMonitored).ToList();

        if (toAdd.Count == 0)
        {
            ShowError("No unmonitored VMs are selected. Check the boxes next to the VMs you want to add.");
            return;
        }

        IsBusy = true;
        try
        {
            // Load current configuration from the service.
            var configResult = await _ipcClient.Service.GetConfigurationAsync(CancellationToken.None);
            if (!configResult.Success)
            {
                ShowError($"Could not read current configuration: {configResult.ErrorMessage}");
                return;
            }

            var configuration = JsonSerializer.Deserialize<FireTowerConfiguration>(
                configResult.Payload!, ConfigurationSerialization.Options)
                ?? new FireTowerConfiguration();

            // Ensure the provider entry exists so the validator accepts the new VMs.
            foreach (var providerId in toAdd.Select(v => v.Vm.ProviderId).Distinct(StringComparer.OrdinalIgnoreCase))
            {
                if (!configuration.Providers.Any(p => string.Equals(p.ProviderId, providerId, StringComparison.OrdinalIgnoreCase)))
                {
                    configuration.Providers.Add(new ProviderOptions { ProviderId = providerId, Enabled = true });
                }
            }

            // Add each selected VM to the configuration, skipping duplicates.
            int added = 0;
            foreach (var item in toAdd)
            {
                var vm = item.Vm;
                if (configuration.VirtualMachines.Any(v =>
                        string.Equals(v.ExternalId, vm.ExternalId, StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(v.ProviderId, vm.ProviderId, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                configuration.VirtualMachines.Add(new VirtualMachineConfig
                {
                    Id = Guid.NewGuid(),
                    ExternalId = vm.ExternalId,
                    Name = vm.Name,
                    ProviderId = vm.ProviderId,
                    Enabled = true,
                    HealthProfileId = ConfigurationDefaults.DefaultHealthProfileId,
                    RecoveryProfileId = ConfigurationDefaults.DefaultRecoveryProfileId,
                });
                added++;
            }

            if (added == 0)
            {
                ShowStatus("The selected VMs are already in your monitoring configuration.");
                return;
            }

            var updatedJson = JsonSerializer.Serialize(configuration, ConfigurationSerialization.Options);
            var saveResult = await _ipcClient.Service.SaveConfigurationAsync(updatedJson, CancellationToken.None);
            if (!saveResult.Success)
            {
                ShowError($"Could not save configuration: {saveResult.ErrorMessage}");
                return;
            }

            // Ask the service to pick up the change immediately.
            await _ipcClient.Service.ReloadConfigurationAsync(CancellationToken.None);

            ShowStatus($"Added {added} VM(s) to monitoring. Go to Virtual Machines to see them.");

            // Refresh so the "AlreadyMonitored" badges update.
            await RunDiscoveryAsync();
        }
        catch (Exception ex)
        {
            ShowError($"Could not add VMs to monitoring: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void RefreshSelectionState()
    {
        HasUnmonitoredSelection = Discovered.Any(v => v.IsSelected && !v.Vm.AlreadyMonitored);
    }
}

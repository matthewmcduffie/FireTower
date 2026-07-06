using System.Collections.ObjectModel;
using System.Text.Json;
using System.Xml.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FireTower.Core.Configuration;
using FireTower.Tray.Services.Ipc;
using Microsoft.Win32;

namespace FireTower.Tray.ViewModels;

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
            ShowError("Not connected to the FireTower service.");
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
                var selectable = new SelectableDiscoveredVm { Vm = vm, IsSelected = !vm.AlreadyMonitored };
                selectable.PropertyChanged += (_, _) => RefreshSelectionState();
                Discovered.Add(selectable);
            }

            RefreshSelectionState();

            ShowStatus(Discovered.Count == 0
                ? "No VMs found. Use Browse for VM to add one directly."
                : $"Found {Discovered.Count} VM(s).");
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
            item.IsSelected = true;
    }

    [RelayCommand]
    private void SelectNone()
    {
        foreach (var item in Discovered)
            item.IsSelected = false;
    }

    [RelayCommand]
    private async Task AddSelectedToMonitoringAsync()
    {
        var toAdd = Discovered.Where(v => v.IsSelected && !v.Vm.AlreadyMonitored).ToList();
        if (toAdd.Count == 0)
        {
            ShowError("No unmonitored VMs are selected.");
            return;
        }

        IsBusy = true;
        try
        {
            var configuration = await LoadConfigurationAsync();
            if (configuration is null) return;

            int added = 0;
            foreach (var item in toAdd)
            {
                var vm = item.Vm;
                if (!configuration.Providers.Any(p => string.Equals(p.ProviderId, vm.ProviderId, StringComparison.OrdinalIgnoreCase)))
                    configuration.Providers.Add(new ProviderOptions { ProviderId = vm.ProviderId, Enabled = true });

                if (configuration.VirtualMachines.Any(v =>
                        string.Equals(v.ExternalId, vm.ExternalId, StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(v.ProviderId, vm.ProviderId, StringComparison.OrdinalIgnoreCase)))
                    continue;

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

            if (added == 0) { ShowStatus("Already in monitoring."); return; }

            if (!await SaveConfigurationAsync(configuration)) return;
            ShowStatus($"Added {added} VM(s) to monitoring.");
            await RunDiscoveryAsync();
        }
        catch (Exception ex)
        {
            ShowError($"Could not add VMs: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task BrowseForVmAsync()
    {
        var dlg = new OpenFileDialog
        {
            Title = "Select a VirtualBox VM file",
            Filter = "VirtualBox VM (*.vbox)|*.vbox|All files (*.*)|*.*",
        };

        if (dlg.ShowDialog() != true)
            return;

        string name;
        string externalId;
        try
        {
            var doc = XDocument.Load(dlg.FileName);
            var ns = doc.Root?.Name.Namespace ?? XNamespace.None;
            var machine = doc.Descendants(ns + "Machine").FirstOrDefault()
                       ?? doc.Descendants("Machine").FirstOrDefault();

            if (machine is null)
            {
                ShowError("Could not read the VM file. Make sure it is a valid VirtualBox .vbox file.");
                return;
            }

            name = machine.Attribute("name")?.Value ?? System.IO.Path.GetFileNameWithoutExtension(dlg.FileName);
            // UUID is stored as {xxxxxxxx-...} — strip the braces
            externalId = (machine.Attribute("uuid")?.Value ?? string.Empty).Trim('{', '}');

            if (string.IsNullOrWhiteSpace(externalId))
            {
                ShowError("The VM file does not contain a UUID. The file may be corrupted.");
                return;
            }
        }
        catch (Exception ex)
        {
            ShowError($"Could not read the VM file: {ex.Message}");
            return;
        }

        IsBusy = true;
        try
        {
            var configuration = await LoadConfigurationAsync();
            if (configuration is null) return;

            if (configuration.VirtualMachines.Any(v =>
                    string.Equals(v.ExternalId, externalId, StringComparison.OrdinalIgnoreCase)))
            {
                ShowStatus($"\"{name}\" is already in monitoring.");
                return;
            }

            if (!configuration.Providers.Any(p => string.Equals(p.ProviderId, "virtualbox", StringComparison.OrdinalIgnoreCase)))
                configuration.Providers.Add(new ProviderOptions { ProviderId = "virtualbox", Enabled = true });

            configuration.VirtualMachines.Add(new VirtualMachineConfig
            {
                Id = Guid.NewGuid(),
                ExternalId = externalId,
                Name = name,
                ProviderId = "virtualbox",
                Enabled = true,
                HealthProfileId = ConfigurationDefaults.DefaultHealthProfileId,
                RecoveryProfileId = ConfigurationDefaults.DefaultRecoveryProfileId,
                VBoxFilePath = dlg.FileName,
            });

            if (!await SaveConfigurationAsync(configuration)) return;
            ShowStatus($"Added \"{name}\" to monitoring. Go to Virtual Machines to see it.");
        }
        catch (Exception ex)
        {
            ShowError($"Could not add VM: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task<FireTowerConfiguration?> LoadConfigurationAsync()
    {
        var result = await _ipcClient.Service.GetConfigurationAsync(CancellationToken.None);
        if (!result.Success)
        {
            ShowError($"Could not read configuration: {result.ErrorMessage}");
            return null;
        }
        return JsonSerializer.Deserialize<FireTowerConfiguration>(result.Payload!, ConfigurationSerialization.Options)
               ?? new FireTowerConfiguration();
    }

    private async Task<bool> SaveConfigurationAsync(FireTowerConfiguration configuration)
    {
        var json = JsonSerializer.Serialize(configuration, ConfigurationSerialization.Options);
        var result = await _ipcClient.Service.SaveConfigurationAsync(json, CancellationToken.None);
        if (!result.Success)
        {
            ShowError($"Could not save configuration: {result.ErrorMessage}");
            return false;
        }
        await _ipcClient.Service.ReloadConfigurationAsync(CancellationToken.None);
        return true;
    }

    private void RefreshSelectionState()
    {
        HasUnmonitoredSelection = Discovered.Any(v => v.IsSelected && !v.Vm.AlreadyMonitored);
    }
}

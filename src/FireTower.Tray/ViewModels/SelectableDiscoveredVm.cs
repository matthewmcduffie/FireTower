using CommunityToolkit.Mvvm.ComponentModel;
using FireTower.Shared.DTOs;

namespace FireTower.Tray.ViewModels;

/// <summary>
/// Wraps a <see cref="DiscoveredVirtualMachineDto"/> with a selection flag so the
/// Discovery page can present a checkbox list for the "Add to Monitoring" workflow.
/// </summary>
public sealed partial class SelectableDiscoveredVm : ObservableObject
{
    [ObservableProperty]
    private bool _isSelected;

    public required DiscoveredVirtualMachineDto Vm { get; init; }
}

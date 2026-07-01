using FireTower.Shared.Enums;

namespace FireTower.Providers.VirtualBox.Parsing;

/// <summary>
/// Translates VirtualBox's native VMState strings into FireTower's provider-independent
/// <see cref="VmPowerState"/>, per the State Mapping table in virtualbox.md.
/// </summary>
public static class VBoxStateMapper
{
    public static VmPowerState Map(string vBoxState) => vBoxState.ToLowerInvariant() switch
    {
        "running" => VmPowerState.Running,
        "poweroff" => VmPowerState.Stopped,
        "paused" => VmPowerState.Paused,
        "saved" => VmPowerState.Saved,
        "starting" => VmPowerState.Starting,
        "stopping" => VmPowerState.Stopping,
        "restoring" => VmPowerState.Restoring,
        "aborted" => VmPowerState.Aborted,
        "gurumeditation" => VmPowerState.Aborted,
        "inaccessible" => VmPowerState.Inaccessible,
        _ => VmPowerState.Unknown,
    };
}

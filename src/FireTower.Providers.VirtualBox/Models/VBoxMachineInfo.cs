namespace FireTower.Providers.VirtualBox.Models;

/// <summary>
/// Fields extracted from <c>VBoxManage showvminfo --machinereadable</c> output.
/// </summary>
public sealed class VBoxMachineInfo
{
    public required string Uuid { get; init; }
    public required string Name { get; init; }
    public required string VmState { get; init; }
    public string? OsType { get; init; }
    public string? ConfigFile { get; init; }
    public int SnapshotCount { get; init; }
}

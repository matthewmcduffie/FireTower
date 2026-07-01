using FireTower.Core.Models;
using FireTower.Providers.VirtualBox.Commands;
using FireTower.Providers.VirtualBox.Parsing;

namespace FireTower.Providers.VirtualBox.Discovery;

/// <summary>
/// Discovers virtual machines via VBoxManage and retrieves per-machine details, per the
/// Virtual Machine Discovery requirements in virtualbox.md. Discovery never modifies
/// VirtualBox state.
/// </summary>
public sealed class VBoxDiscoveryService
{
    private readonly IVBoxCommandRunner _commandRunner;
    private readonly Func<TimeSpan> _timeoutProvider;

    public VBoxDiscoveryService(IVBoxCommandRunner commandRunner, Func<TimeSpan> timeoutProvider)
    {
        _commandRunner = commandRunner;
        _timeoutProvider = timeoutProvider;
    }

    public async Task<IReadOnlyList<DiscoveredVirtualMachine>> DiscoverAsync(CancellationToken cancellationToken)
    {
        var timeout = _timeoutProvider();
        var listResult = await _commandRunner.RunAsync(new[] { "list", "vms" }, timeout, cancellationToken).ConfigureAwait(false);
        if (!listResult.Succeeded)
        {
            return Array.Empty<DiscoveredVirtualMachine>();
        }

        var entries = VmListParser.Parse(listResult.StandardOutput);
        var discovered = new List<DiscoveredVirtualMachine>(entries.Count);
        var now = DateTimeOffset.UtcNow;

        foreach (var (_, uuid) in entries)
        {
            var infoResult = await _commandRunner.RunAsync(
                new[] { "showvminfo", uuid, "--machinereadable" }, timeout, cancellationToken).ConfigureAwait(false);

            if (!infoResult.Succeeded)
            {
                continue;
            }

            var fields = MachineReadableParser.ParseKeyValuePairs(infoResult.StandardOutput);
            var info = MachineReadableParser.ToMachineInfo(fields);

            discovered.Add(new DiscoveredVirtualMachine
            {
                ExternalId = info.Uuid,
                Name = info.Name,
                ProviderId = "virtualbox",
                PowerState = VBoxStateMapper.Map(info.VmState),
                OperatingSystem = info.OsType,
                ConfigurationPath = info.ConfigFile,
                HasSnapshots = info.SnapshotCount > 0,
                DiscoveredAt = now,
            });
        }

        return discovered;
    }

    public async Task<FireTower.Shared.Enums.VmPowerState> GetStateAsync(string externalId, CancellationToken cancellationToken)
    {
        var result = await _commandRunner.RunAsync(
            new[] { "showvminfo", externalId, "--machinereadable" }, _timeoutProvider(), cancellationToken).ConfigureAwait(false);

        if (!result.Succeeded)
        {
            return FireTower.Shared.Enums.VmPowerState.Unknown;
        }

        var fields = MachineReadableParser.ParseKeyValuePairs(result.StandardOutput);
        return VBoxStateMapper.Map(fields.GetValueOrDefault("VMState", "unknown"));
    }
}

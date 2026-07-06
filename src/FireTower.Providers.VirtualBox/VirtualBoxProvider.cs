using FireTower.Core.Interfaces;
using FireTower.Core.Models;
using FireTower.Providers.VirtualBox.Commands;
using FireTower.Providers.VirtualBox.Discovery;
using FireTower.Shared.Enums;
using FireTower.Shared.Exceptions;
using Microsoft.Extensions.Logging;

namespace FireTower.Providers.VirtualBox;

/// <summary>
/// VirtualBox implementation of <see cref="IVmProvider"/>, per virtualbox.md. Responsible
/// only for communicating with VirtualBox; restart decisions, scheduling, and business
/// rules live elsewhere.
/// </summary>
public sealed class VirtualBoxProvider : IVmProvider
{
    public const string Id = "virtualbox";

    public string ProviderId => Id;
    public string FriendlyName => "Oracle VirtualBox";

    private readonly IVBoxCommandRunner _commandRunner;
    private readonly IConfigurationManager _configurationManager;
    private readonly ILogger<VirtualBoxProvider> _logger;
    private readonly VBoxDiscoveryService _discoveryService;

    private string? _detectedVersion;

    public VirtualBoxProvider(IVBoxCommandRunner commandRunner, IConfigurationManager configurationManager, ILogger<VirtualBoxProvider> logger)
    {
        _commandRunner = commandRunner;
        _configurationManager = configurationManager;
        _logger = logger;
        _discoveryService = new VBoxDiscoveryService(_commandRunner, () => OperationTimeout);
    }

    private TimeSpan OperationTimeout
    {
        get
        {
            var seconds = _configurationManager.Current.Providers
                .FirstOrDefault(p => string.Equals(p.ProviderId, Id, StringComparison.OrdinalIgnoreCase))
                ?.OperationTimeoutSeconds ?? 30;
            return TimeSpan.FromSeconds(seconds);
        }
    }

    /// <summary>
    /// VBoxManage start type: "gui" shows the VM window (default), "headless" runs
    /// in the background with no display. Configurable via providers.json VmStartType.
    /// </summary>
    private string VmStartType =>
        _configurationManager.Current.Providers
            .FirstOrDefault(p => string.Equals(p.ProviderId, Id, StringComparison.OrdinalIgnoreCase))
            ?.VmStartType ?? "gui";

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        var result = await _commandRunner.RunAsync(new[] { "--version" }, OperationTimeout, cancellationToken).ConfigureAwait(false);
        if (!result.Succeeded)
        {
            throw new ProviderException(ProviderId, $"VBoxManage --version failed: {result.StandardError}");
        }

        _detectedVersion = result.StandardOutput.Trim();

        // Probe user profile directories to find the right VBOX_USER_HOME. This runs
        // actual VBoxManage commands rather than just checking for file existence, so
        // it works even when VMs are in non-default or custom locations.
        await _commandRunner.CalibrateVBoxUserHomeAsync(OperationTimeout, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("VirtualBox provider initialized, detected version {Version}", _detectedVersion);
    }

    public Task ShutdownAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task<ProviderRegistration> GetCapabilitiesAsync(CancellationToken cancellationToken) =>
        Task.FromResult(new ProviderRegistration
        {
            ProviderId = ProviderId,
            FriendlyName = FriendlyName,
            Version = _detectedVersion ?? "unknown",
            Capabilities = new[]
            {
                ProviderCapability.Discovery,
                ProviderCapability.PowerControl,
                ProviderCapability.Snapshots,
                ProviderCapability.HealthStatus,
                ProviderCapability.GuestInformation,
                ProviderCapability.GracefulShutdown,
                ProviderCapability.Suspend,
                ProviderCapability.Resume,
            },
        });

    public Task<IReadOnlyList<DiscoveredVirtualMachine>> DiscoverVirtualMachinesAsync(CancellationToken cancellationToken) =>
        _discoveryService.DiscoverAsync(cancellationToken);

    public Task<VmPowerState> GetStateAsync(string externalId, CancellationToken cancellationToken) =>
        _discoveryService.GetStateAsync(externalId, cancellationToken);

    public async Task<VmRuntimeInfo> GetRuntimeInfoAsync(string externalId, CancellationToken cancellationToken)
    {
        var state = await GetStateAsync(externalId, cancellationToken).ConfigureAwait(false);
        return new VmRuntimeInfo
        {
            ExternalId = externalId,
            PowerState = state,
            RetrievedAt = DateTimeOffset.UtcNow,
        };
    }

    public Task StartAsync(string externalId, CancellationToken cancellationToken) =>
        RunControlCommandAsync(new[] { "startvm", externalId, "--type", VmStartType }, cancellationToken);

    public Task StopAsync(string externalId, CancellationToken cancellationToken) =>
        RunControlCommandAsync(new[] { "controlvm", externalId, "poweroff" }, cancellationToken);

    public Task PowerOffAsync(string externalId, CancellationToken cancellationToken) =>
        RunControlCommandAsync(new[] { "controlvm", externalId, "poweroff" }, cancellationToken);

    public Task GracefulShutdownAsync(string externalId, CancellationToken cancellationToken) =>
        RunControlCommandAsync(new[] { "controlvm", externalId, "acpipowerbutton" }, cancellationToken);

    public async Task RestartAsync(string externalId, CancellationToken cancellationToken)
    {
        // If the VM is already stopped there is nothing to shut down — skip straight
        // to Start.  This covers the common monitoring recovery case where FireTower
        // detects the VM stopped unexpectedly and wants to bring it back.
        var state = await GetStateAsync(externalId, cancellationToken).ConfigureAwait(false);
        if (state == VmPowerState.Running || state == VmPowerState.Paused)
        {
            await GracefulShutdownAsync(externalId, cancellationToken).ConfigureAwait(false);
            // Brief pause to let the OS acknowledge the ACPI signal before issuing startvm
            await Task.Delay(TimeSpan.FromSeconds(3), cancellationToken).ConfigureAwait(false);
        }
        await StartAsync(externalId, cancellationToken).ConfigureAwait(false);
    }

    public async Task ForceRestartAsync(string externalId, CancellationToken cancellationToken)
    {
        var state = await GetStateAsync(externalId, cancellationToken).ConfigureAwait(false);
        if (state != VmPowerState.Stopped && state != VmPowerState.Aborted)
        {
            await PowerOffAsync(externalId, cancellationToken).ConfigureAwait(false);
        }
        await StartAsync(externalId, cancellationToken).ConfigureAwait(false);
    }

    private async Task RunControlCommandAsync(IReadOnlyList<string> arguments, CancellationToken cancellationToken)
    {
        var result = await _commandRunner.RunAsync(arguments, OperationTimeout, cancellationToken).ConfigureAwait(false);
        if (!result.Succeeded)
        {
            throw new ProviderException(ProviderId, $"VBoxManage {string.Join(' ', arguments)} failed: {result.StandardError}");
        }
    }
}

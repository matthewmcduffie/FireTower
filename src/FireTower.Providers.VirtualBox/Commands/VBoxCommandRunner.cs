using System.Diagnostics;
using System.Runtime.Versioning;
using System.Text;
using FireTower.Core.Interfaces;
using FireTower.Providers.VirtualBox.Platform;
using FireTower.Providers.VirtualBox.Services;
using FireTower.Shared.Exceptions;
using Microsoft.Extensions.Logging;

namespace FireTower.Providers.VirtualBox.Commands;

/// <summary>
/// Runs VBoxManage and captures stdout/stderr/exit code.
/// When the service is running as LocalSystem, VBoxManage is launched in the active
/// user's session via CreateProcessAsUser so it can reach VBoxSVC (which manages
/// runtime VM state and runs in the user's session, not Session 0).
/// </summary>
public sealed class VBoxCommandRunner : IVBoxCommandRunner
{
    private readonly IVBoxManageLocator _locator;
    private readonly IConfigurationManager _configurationManager;
    private readonly ILogger<VBoxCommandRunner> _logger;

    public VBoxCommandRunner(IVBoxManageLocator locator, IConfigurationManager configurationManager, ILogger<VBoxCommandRunner> logger)
    {
        _locator = locator;
        _configurationManager = configurationManager;
        _logger = logger;
    }

    public Task<VBoxCommandResult> RunAsync(IReadOnlyList<string> arguments, TimeSpan timeout, CancellationToken cancellationToken)
    {
        var providerOptions = _configurationManager.Current.Providers
            .FirstOrDefault(p => string.Equals(p.ProviderId, VirtualBoxProvider.Id, StringComparison.OrdinalIgnoreCase));

        var executablePath = _locator.Locate(providerOptions?.ExecutablePath);

        var stopwatch = Stopwatch.StartNew();

        // Run VBoxManage in the active user's session so it can reach VBoxSVC.
        // VBoxSVC manages runtime VM state and lives in the user's interactive session.
        // A service running in Session 0 cannot reach it via COM directly.
        var (exitCode, stdout, stderr) = UserSessionRunner.Run(executablePath, arguments, timeout);

        stopwatch.Stop();

        var result = new VBoxCommandResult(exitCode, stdout, stderr, stopwatch.Elapsed);
        _logger.LogDebug("VBoxManage {Arguments} completed in {Duration}ms exit {ExitCode}",
            string.Join(' ', arguments), result.Duration.TotalMilliseconds, result.ExitCode);

        return Task.FromResult(result);
    }

    public Task CalibrateVBoxUserHomeAsync(TimeSpan timeout, CancellationToken cancellationToken)
        => Task.CompletedTask; // No longer needed — VBoxManage runs as the user directly.

    private void TryKill(Process process)
    {
        try { if (!process.HasExited) process.Kill(entireProcessTree: true); }
        catch (Exception ex) { _logger.LogWarning(ex, "Failed to terminate VBoxManage process"); }
    }
}

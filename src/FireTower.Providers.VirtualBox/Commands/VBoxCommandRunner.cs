using System.Diagnostics;
using System.Text;
using FireTower.Core.Interfaces;
using FireTower.Providers.VirtualBox.Services;
using FireTower.Shared.Exceptions;
using Microsoft.Extensions.Logging;

namespace FireTower.Providers.VirtualBox.Commands;

/// <summary>
/// Default <see cref="IVBoxCommandRunner"/> implementation: launches VBoxManage as a
/// child process, captures stdout/stderr/exit code, and enforces the requested timeout
/// by killing the process tree rather than waiting indefinitely. The configured executable
/// path is read from configuration on every call rather than captured at construction time,
/// since the provider can be constructed before configuration has finished loading.
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

    public async Task<VBoxCommandResult> RunAsync(IReadOnlyList<string> arguments, TimeSpan timeout, CancellationToken cancellationToken)
    {
        var configuredPath = _configurationManager.Current.Providers
            .FirstOrDefault(p => string.Equals(p.ProviderId, VirtualBoxProvider.Id, StringComparison.OrdinalIgnoreCase))
            ?.ExecutablePath;

        var executablePath = _locator.Locate(configuredPath);

        var startInfo = new ProcessStartInfo(executablePath)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = new Process { StartInfo = startInfo };
        var stopwatch = Stopwatch.StartNew();
        var stdout = new StringBuilder();
        var stderr = new StringBuilder();

        process.OutputDataReceived += (_, e) => { if (e.Data is not null) stdout.AppendLine(e.Data); };
        process.ErrorDataReceived += (_, e) => { if (e.Data is not null) stderr.AppendLine(e.Data); };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        using var timeoutSource = new CancellationTokenSource(timeout);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutSource.Token);

        try
        {
            await process.WaitForExitAsync(linked.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            TryKill(process);
            if (timeoutSource.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
            {
                throw new ProviderException("virtualbox", $"VBoxManage {string.Join(' ', arguments)} timed out after {timeout}.");
            }

            throw;
        }

        stopwatch.Stop();

        var result = new VBoxCommandResult(process.ExitCode, stdout.ToString(), stderr.ToString(), stopwatch.Elapsed);
        _logger.LogDebug("VBoxManage {Arguments} completed in {Duration}ms with exit code {ExitCode}",
            string.Join(' ', arguments), result.Duration.TotalMilliseconds, result.ExitCode);

        return result;
    }

    private void TryKill(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to terminate timed-out VBoxManage process");
        }
    }
}

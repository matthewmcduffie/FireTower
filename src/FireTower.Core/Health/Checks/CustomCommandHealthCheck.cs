using System.Diagnostics;
using FireTower.Core.Interfaces;
using FireTower.Core.Models;
using FireTower.Shared.Enums;

namespace FireTower.Core.Health.Checks;

/// <summary>
/// Runs an administrator-defined command or script on the FireTower host, per
/// health-engine.md. Exit code 0 means Healthy, 1 means Warning, anything else means
/// Failed, matching the "Healthy / Warning / Failed / Unknown" contract the check defines.
/// </summary>
public sealed class CustomCommandHealthCheck : IHealthCheck
{
    public HealthCheckKind Kind => HealthCheckKind.CustomCommand;

    public async Task<HealthCheckResult> ExecuteAsync(VirtualMachine virtualMachine, HealthCheckDefinition definition, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        if (!definition.Parameters.TryGetValue("command", out var command) || string.IsNullOrWhiteSpace(command))
        {
            return HealthCheckResultFactory.Create(definition.Id, HealthCheckOutcome.Unknown, stopwatch.Elapsed, "No 'command' parameter configured.");
        }

        var arguments = definition.Parameters.GetValueOrDefault("arguments", string.Empty);

        using var process = new Process
        {
            StartInfo = new ProcessStartInfo(command, arguments)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            },
        };

        using var timeoutSource = new CancellationTokenSource(TimeSpan.FromSeconds(definition.TimeoutSeconds));
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutSource.Token);

        try
        {
            process.Start();
            await process.WaitForExitAsync(linked.Token).ConfigureAwait(false);

            var outcome = process.ExitCode switch
            {
                0 => HealthCheckOutcome.Healthy,
                1 => HealthCheckOutcome.Warning,
                _ => HealthCheckOutcome.Failed,
            };

            return HealthCheckResultFactory.Create(definition.Id, outcome, stopwatch.Elapsed, $"Exit code {process.ExitCode}.");
        }
        catch (OperationCanceledException) when (timeoutSource.IsCancellationRequested)
        {
            TryKill(process);
            return HealthCheckResultFactory.Create(definition.Id, HealthCheckOutcome.Timeout, stopwatch.Elapsed, "Custom command timed out.");
        }
        catch (Exception ex)
        {
            return HealthCheckResultFactory.Create(definition.Id, HealthCheckOutcome.Failed, stopwatch.Elapsed, ex.Message);
        }
    }

    private static void TryKill(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch (InvalidOperationException)
        {
        }
    }
}

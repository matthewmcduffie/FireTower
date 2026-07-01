using FireTower.Core.Interfaces;
using FireTower.Core.Models;
using FireTower.Shared.Enums;

namespace FireTower.Core.Health;

/// <summary>
/// Runs a single health check with retries, per the Retry Logic requirements in
/// health-engine.md: only after every attempt fails does the check report failure.
/// </summary>
public sealed class HealthCheckExecutor
{
    public async Task<HealthCheckResult> ExecuteWithRetriesAsync(
        IHealthCheck check, VirtualMachine virtualMachine, HealthCheckDefinition definition, CancellationToken cancellationToken)
    {
        var attempts = Math.Max(1, definition.RetryCount + 1);

        for (var attempt = 1; ; attempt++)
        {
            var result = await check.ExecuteAsync(virtualMachine, definition, cancellationToken).ConfigureAwait(false);

            var isHardFailure = result.Outcome is HealthCheckOutcome.Failed or HealthCheckOutcome.Timeout;
            if (!isHardFailure || attempt >= attempts)
            {
                return result;
            }
        }
    }
}

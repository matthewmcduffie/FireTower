using System.Diagnostics;
using System.Net.NetworkInformation;
using FireTower.Core.Interfaces;
using FireTower.Core.Models;
using FireTower.Shared.Enums;

namespace FireTower.Core.Health.Checks;

/// <summary>
/// ICMP ping check, per health-engine.md. Configured via a "target" parameter (hostname
/// or IP address). A successful ping alone does not guarantee a healthy VM; it is one
/// input among several combined by the evaluation rules.
/// </summary>
public sealed class PingHealthCheck : IHealthCheck
{
    public HealthCheckKind Kind => HealthCheckKind.Ping;

    public async Task<HealthCheckResult> ExecuteAsync(VirtualMachine virtualMachine, HealthCheckDefinition definition, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        if (!definition.Parameters.TryGetValue("target", out var target) || string.IsNullOrWhiteSpace(target))
        {
            return HealthCheckResultFactory.Create(definition.Id, HealthCheckOutcome.Unknown, stopwatch.Elapsed, "No 'target' parameter configured.");
        }

        try
        {
            using var ping = new Ping();
            var reply = await ping.SendPingAsync(target, definition.TimeoutSeconds * 1000).ConfigureAwait(false);
            var outcome = reply.Status == IPStatus.Success ? HealthCheckOutcome.Healthy : HealthCheckOutcome.Failed;
            return HealthCheckResultFactory.Create(definition.Id, outcome, stopwatch.Elapsed, $"Ping to {target}: {reply.Status}.");
        }
        catch (PingException ex)
        {
            return HealthCheckResultFactory.Create(definition.Id, HealthCheckOutcome.Failed, stopwatch.Elapsed, ex.Message);
        }
    }
}

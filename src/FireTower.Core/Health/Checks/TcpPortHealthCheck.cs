using System.Diagnostics;
using System.Net.Sockets;
using FireTower.Core.Interfaces;
using FireTower.Core.Models;
using FireTower.Shared.Enums;

namespace FireTower.Core.Health.Checks;

/// <summary>
/// Attempts a TCP connection to a configured host and port, per health-engine.md.
/// A successful connection indicates the service is listening; it does not validate
/// the service's own behavior.
/// </summary>
public sealed class TcpPortHealthCheck : IHealthCheck
{
    public HealthCheckKind Kind => HealthCheckKind.TcpPort;

    public async Task<HealthCheckResult> ExecuteAsync(VirtualMachine virtualMachine, HealthCheckDefinition definition, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        if (!definition.Parameters.TryGetValue("host", out var host) || string.IsNullOrWhiteSpace(host) ||
            !definition.Parameters.TryGetValue("port", out var portText) || !int.TryParse(portText, out var port))
        {
            return HealthCheckResultFactory.Create(definition.Id, HealthCheckOutcome.Unknown, stopwatch.Elapsed, "Missing or invalid 'host'/'port' parameters.");
        }

        using var client = new TcpClient();
        using var timeoutSource = new CancellationTokenSource(TimeSpan.FromSeconds(definition.TimeoutSeconds));
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutSource.Token);

        try
        {
            await client.ConnectAsync(host, port, linked.Token).ConfigureAwait(false);
            return HealthCheckResultFactory.Create(definition.Id, HealthCheckOutcome.Healthy, stopwatch.Elapsed, $"Connected to {host}:{port}.");
        }
        catch (OperationCanceledException) when (timeoutSource.IsCancellationRequested)
        {
            return HealthCheckResultFactory.Create(definition.Id, HealthCheckOutcome.Timeout, stopwatch.Elapsed, $"Connection to {host}:{port} timed out.");
        }
        catch (SocketException ex)
        {
            return HealthCheckResultFactory.Create(definition.Id, HealthCheckOutcome.Failed, stopwatch.Elapsed, ex.Message);
        }
    }
}

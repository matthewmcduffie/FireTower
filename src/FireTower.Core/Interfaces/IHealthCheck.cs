using FireTower.Core.Models;
using FireTower.Shared.Enums;

namespace FireTower.Core.Interfaces;

/// <summary>
/// A single health check strategy (Ping, TCP, Guest Command, etc.). The Health Engine
/// resolves the implementation matching a <see cref="HealthCheckDefinition.Kind"/> and
/// executes it; new check kinds can be added without modifying the engine itself.
/// </summary>
public interface IHealthCheck
{
    HealthCheckKind Kind { get; }

    Task<HealthCheckResult> ExecuteAsync(
        VirtualMachine virtualMachine,
        HealthCheckDefinition definition,
        CancellationToken cancellationToken);
}

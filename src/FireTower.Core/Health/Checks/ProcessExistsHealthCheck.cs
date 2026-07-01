using FireTower.Core.Interfaces;
using FireTower.Core.Models;
using FireTower.Shared.Enums;

namespace FireTower.Core.Health.Checks;

/// <summary>
/// Verifies that an expected process is running inside the guest, per health-engine.md.
/// Like <see cref="GuestCommandHealthCheck"/>, this requires guest execution support that
/// no provider currently implements, so it reports <see cref="HealthCheckOutcome.Unsupported"/>
/// rather than checking processes on the FireTower host, which would be misleading.
/// </summary>
public sealed class ProcessExistsHealthCheck : IHealthCheck
{
    public HealthCheckKind Kind => HealthCheckKind.ProcessExists;

    public Task<HealthCheckResult> ExecuteAsync(VirtualMachine virtualMachine, HealthCheckDefinition definition, CancellationToken cancellationToken) =>
        Task.FromResult(HealthCheckResultFactory.Create(
            definition.Id, HealthCheckOutcome.Unsupported, TimeSpan.Zero,
            "Guest process inspection is not yet supported by any registered provider."));
}

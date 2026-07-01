using FireTower.Core.Interfaces;
using FireTower.Core.Models;
using FireTower.Shared.Enums;

namespace FireTower.Core.Health.Checks;

/// <summary>
/// Runs a command inside the guest operating system, per health-engine.md. Guest command
/// execution requires provider support that is not yet implemented by any provider
/// (see virtualbox.md's Future Improvements); until a provider advertises this capability,
/// the check reports <see cref="HealthCheckOutcome.Unsupported"/> rather than guessing.
/// </summary>
public sealed class GuestCommandHealthCheck : IHealthCheck
{
    public HealthCheckKind Kind => HealthCheckKind.GuestCommand;

    public Task<HealthCheckResult> ExecuteAsync(VirtualMachine virtualMachine, HealthCheckDefinition definition, CancellationToken cancellationToken) =>
        Task.FromResult(HealthCheckResultFactory.Create(
            definition.Id, HealthCheckOutcome.Unsupported, TimeSpan.Zero,
            "Guest command execution is not yet supported by any registered provider."));
}

using FireTower.Shared.Enums;

namespace FireTower.Core.Models;

/// <summary>
/// The combined result of one health-check pipeline cycle for a single virtual machine:
/// the overall health state plus the individual check results that produced it.
/// </summary>
public sealed class HealthEvaluation
{
    public required Guid VirtualMachineId { get; init; }
    public required HealthState PreviousState { get; init; }
    public required HealthState NewState { get; init; }
    public required IReadOnlyList<HealthCheckResult> CheckResults { get; init; }
    public required DateTimeOffset Timestamp { get; init; }

    public bool StateChanged => PreviousState != NewState;
}

using FireTower.Core.Models;

namespace FireTower.Core.Interfaces;

/// <summary>
/// Evaluates the health of monitored virtual machines. The Health Engine reports health;
/// it never performs recovery (see restart-engine.md for that separation).
/// </summary>
public interface IHealthEngine
{
    Task<HealthEvaluation> EvaluateAsync(VirtualMachine virtualMachine, HealthProfile profile, CancellationToken cancellationToken);
}

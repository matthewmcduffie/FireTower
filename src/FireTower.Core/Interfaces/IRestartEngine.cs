using FireTower.Core.Models;

namespace FireTower.Core.Interfaces;

/// <summary>
/// Converts health evaluations into recovery decisions and executes them. The Restart Engine
/// never performs health checks (see health-engine.md for that separation).
/// </summary>
public interface IRestartEngine
{
    Task<RecoveryDecision> EvaluateAsync(VirtualMachine virtualMachine, RecoveryProfile profile, HealthEvaluation evaluation, CancellationToken cancellationToken);

    Task<RecoveryResult> ExecuteAsync(VirtualMachine virtualMachine, RecoveryDecision decision, CancellationToken cancellationToken);
}

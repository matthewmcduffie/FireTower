using FireTower.Core.Models;
using FireTower.Shared.Enums;

namespace FireTower.Core.Health;

/// <summary>
/// Combines per-check classifications into one overall <see cref="HealthState"/>, per the
/// Evaluation table in health-engine.md. Kept as a pure function so the combination logic
/// is independently testable without executing real health checks.
/// </summary>
public static class HealthEvaluationRules
{
    public static HealthState Combine(IReadOnlyList<(HealthCheckDefinition Definition, HealthCheckResult Result, HealthCheckClassification Classification)> checks)
    {
        var evaluable = checks
            .Where(c => c.Result.Outcome is not (HealthCheckOutcome.Unsupported or HealthCheckOutcome.Unknown))
            .ToList();

        if (evaluable.Count == 0)
        {
            return HealthState.Unknown;
        }

        var providerCheck = evaluable.FirstOrDefault(c => c.Definition.Kind == HealthCheckKind.ProviderStatus);
        if (providerCheck.Definition is not null && providerCheck.Classification == HealthCheckClassification.Failing)
        {
            return HealthState.Critical;
        }

        var others = evaluable.Where(c => c.Definition.Kind != HealthCheckKind.ProviderStatus).ToList();
        if (others.Count == 0)
        {
            return HealthState.Healthy;
        }

        if (others.All(c => c.Classification == HealthCheckClassification.Failing))
        {
            return HealthState.Critical;
        }

        if (others.Any(c => c.Classification == HealthCheckClassification.Failing))
        {
            return HealthState.Warning;
        }

        if (others.Any(c => c.Classification == HealthCheckClassification.Recovering))
        {
            return HealthState.Recovering;
        }

        return others.Any(c => c.Result.Outcome == HealthCheckOutcome.Warning)
            ? HealthState.Warning
            : HealthState.Healthy;
    }
}

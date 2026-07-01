using FireTower.Core.Interfaces;
using FireTower.Core.Models;
using Microsoft.Extensions.Logging;

namespace FireTower.Core.Health;

/// <summary>
/// Default <see cref="IHealthEngine"/> implementation: runs every enabled check in a
/// Health Profile, applies failure/recovery thresholds, and combines the results into one
/// overall health state, per health-engine.md. Never performs recovery actions.
/// </summary>
public sealed class HealthEngine : IHealthEngine
{
    private readonly IReadOnlyDictionary<Shared.Enums.HealthCheckKind, IHealthCheck> _checksByKind;
    private readonly HealthCheckStateTracker _stateTracker;
    private readonly HealthCheckExecutor _executor;
    private readonly ILogger<HealthEngine> _logger;

    public HealthEngine(
        IEnumerable<IHealthCheck> healthChecks,
        HealthCheckStateTracker stateTracker,
        HealthCheckExecutor executor,
        ILogger<HealthEngine> logger)
    {
        _checksByKind = healthChecks.ToDictionary(c => c.Kind);
        _stateTracker = stateTracker;
        _executor = executor;
        _logger = logger;
    }

    public async Task<HealthEvaluation> EvaluateAsync(VirtualMachine virtualMachine, HealthProfile profile, CancellationToken cancellationToken)
    {
        var previousState = virtualMachine.Health;
        var results = new List<HealthCheckResult>();
        var classified = new List<(HealthCheckDefinition Definition, HealthCheckResult Result, HealthCheckClassification Classification)>();

        foreach (var definition in profile.Checks.Where(c => c.Enabled))
        {
            if (!_checksByKind.TryGetValue(definition.Kind, out var check))
            {
                _logger.LogWarning("No health check implementation registered for {Kind}", definition.Kind);
                continue;
            }

            var result = await _executor.ExecuteWithRetriesAsync(check, virtualMachine, definition, cancellationToken).ConfigureAwait(false);
            results.Add(result);

            var state = _stateTracker.GetState(virtualMachine.Id, definition.Id);
            var classification = state.Update(result, definition);
            classified.Add((definition, result, classification));
        }

        var newState = HealthEvaluationRules.Combine(classified);

        return new HealthEvaluation
        {
            VirtualMachineId = virtualMachine.Id,
            PreviousState = previousState,
            NewState = newState,
            CheckResults = results,
            Timestamp = DateTimeOffset.UtcNow,
        };
    }
}

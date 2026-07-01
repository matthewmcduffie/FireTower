using FireTower.Core.Models;
using FireTower.Shared.Enums;

namespace FireTower.Core.Health;

/// <summary>
/// Tracks consecutive failures and successes for a single (virtual machine, health check)
/// pair, applying the Failure Threshold / Recovery Threshold hysteresis described in
/// health-engine.md: once a check crosses its failure threshold it stays classified as
/// failing until it crosses its recovery threshold, rather than flapping on every sample.
/// </summary>
public sealed class ConsecutiveCheckState
{
    private int _consecutiveFailures;
    private int _consecutiveSuccesses;
    private bool _isInFailedState;

    public HealthCheckOutcome LastOutcome { get; private set; } = HealthCheckOutcome.Unknown;

    public HealthCheckClassification Update(HealthCheckResult result, HealthCheckDefinition definition)
    {
        LastOutcome = result.Outcome;

        var isHardFailure = result.Outcome is HealthCheckOutcome.Failed or HealthCheckOutcome.Timeout;

        if (isHardFailure)
        {
            _consecutiveFailures++;
            _consecutiveSuccesses = 0;
        }
        else
        {
            _consecutiveSuccesses++;
            _consecutiveFailures = 0;
        }

        if (!_isInFailedState && _consecutiveFailures >= definition.FailureThreshold)
        {
            _isInFailedState = true;
        }
        else if (_isInFailedState && _consecutiveSuccesses >= definition.RecoveryThreshold)
        {
            _isInFailedState = false;
        }

        if (!_isInFailedState)
        {
            // A failure that hasn't yet crossed the threshold should not change health
            // state on its own (temporary issues should not immediately trigger recovery).
            return HealthCheckClassification.Passing;
        }

        return _consecutiveSuccesses > 0
            ? HealthCheckClassification.Recovering
            : HealthCheckClassification.Failing;
    }
}

using FireTower.Core.Health;
using FireTower.Core.Models;
using FireTower.Shared.Enums;

namespace FireTower.Tests.Health;

/// <summary>
/// Unit tests for <see cref="ConsecutiveCheckState"/>, verifying that failure and
/// recovery thresholds produce the correct classification transitions per health-engine.md.
/// </summary>
public sealed class ConsecutiveCheckStateTests
{
    private static HealthCheckDefinition Definition(int failureThreshold, int recoveryThreshold) => new()
    {
        Id = "test",
        Kind = HealthCheckKind.Ping,
        FailureThreshold = failureThreshold,
        RecoveryThreshold = recoveryThreshold,
    };

    private static HealthCheckResult Passed() => new()
    {
        HealthCheckId = "test",
        Outcome = HealthCheckOutcome.Healthy,
        Duration = TimeSpan.Zero,
        Timestamp = DateTimeOffset.UtcNow,
    };

    private static HealthCheckResult Failed() => new()
    {
        HealthCheckId = "test",
        Outcome = HealthCheckOutcome.Failed,
        Duration = TimeSpan.Zero,
        Timestamp = DateTimeOffset.UtcNow,
    };

    [Fact]
    public void State_StartsAsPassing_BeforeThresholdCrossed()
    {
        var state = new ConsecutiveCheckState();
        var definition = Definition(failureThreshold: 3, recoveryThreshold: 2);

        var result = state.Update(Failed(), definition);

        Assert.Equal(HealthCheckClassification.Passing, result);
    }

    [Fact]
    public void State_BecomesFailingOnce_FailureThresholdCrossed()
    {
        var state = new ConsecutiveCheckState();
        var definition = Definition(failureThreshold: 3, recoveryThreshold: 2);

        state.Update(Failed(), definition);
        state.Update(Failed(), definition);
        var result = state.Update(Failed(), definition);

        Assert.Equal(HealthCheckClassification.Failing, result);
    }

    [Fact]
    public void State_BecomesRecovering_AfterFirstSuccessWhileFailing()
    {
        var state = new ConsecutiveCheckState();
        var definition = Definition(failureThreshold: 3, recoveryThreshold: 2);

        state.Update(Failed(), definition);
        state.Update(Failed(), definition);
        state.Update(Failed(), definition);
        var result = state.Update(Passed(), definition);

        Assert.Equal(HealthCheckClassification.Recovering, result);
    }

    [Fact]
    public void State_BecomesPassingOnce_RecoveryThresholdCrossed()
    {
        var state = new ConsecutiveCheckState();
        var definition = Definition(failureThreshold: 3, recoveryThreshold: 2);

        state.Update(Failed(), definition);
        state.Update(Failed(), definition);
        state.Update(Failed(), definition);
        state.Update(Passed(), definition);
        var result = state.Update(Passed(), definition);

        Assert.Equal(HealthCheckClassification.Passing, result);
    }

    [Fact]
    public void State_RemainsPassingForTransientFailures_UnderThreshold()
    {
        var state = new ConsecutiveCheckState();
        var definition = Definition(failureThreshold: 3, recoveryThreshold: 2);

        state.Update(Passed(), definition);
        state.Update(Failed(), definition);
        var result = state.Update(Passed(), definition);

        Assert.Equal(HealthCheckClassification.Passing, result);
    }

    [Fact]
    public void State_RequiresFullRecoveryThreshold_NotSingleSuccess()
    {
        var state = new ConsecutiveCheckState();
        var definition = Definition(failureThreshold: 2, recoveryThreshold: 3);

        state.Update(Failed(), definition);
        state.Update(Failed(), definition);
        state.Update(Passed(), definition);
        state.Update(Passed(), definition);
        var result = state.Update(Passed(), definition);

        Assert.Equal(HealthCheckClassification.Passing, result);
    }
}

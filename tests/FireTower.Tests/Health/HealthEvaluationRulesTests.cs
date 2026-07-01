using FireTower.Core.Health;
using FireTower.Core.Models;
using FireTower.Shared.Enums;

namespace FireTower.Tests.Health;

/// <summary>
/// Unit tests for <see cref="HealthEvaluationRules.Combine"/>, verifying the state
/// combination table from health-engine.md. Pure function — no I/O.
/// </summary>
public sealed class HealthEvaluationRulesTests
{
    private static (HealthCheckDefinition, HealthCheckResult, HealthCheckClassification) Entry(
        HealthCheckKind kind, HealthCheckOutcome outcome, HealthCheckClassification classification) =>
        (
            new HealthCheckDefinition { Id = kind.ToString(), Kind = kind },
            new HealthCheckResult { HealthCheckId = kind.ToString(), Outcome = outcome, Duration = TimeSpan.Zero, Timestamp = DateTimeOffset.UtcNow },
            classification
        );

    [Fact]
    public void Combine_ReturnsCritical_WhenProviderStatusFailing()
    {
        var checks = new[]
        {
            Entry(HealthCheckKind.ProviderStatus, HealthCheckOutcome.Failed, HealthCheckClassification.Failing),
        };

        Assert.Equal(HealthState.Critical, HealthEvaluationRules.Combine(checks));
    }

    [Fact]
    public void Combine_ReturnsHealthy_WhenAllChecksPassing()
    {
        var checks = new[]
        {
            Entry(HealthCheckKind.ProviderStatus, HealthCheckOutcome.Healthy, HealthCheckClassification.Passing),
            Entry(HealthCheckKind.Ping, HealthCheckOutcome.Healthy, HealthCheckClassification.Passing),
        };

        Assert.Equal(HealthState.Healthy, HealthEvaluationRules.Combine(checks));
    }

    [Fact]
    public void Combine_ReturnsCritical_WhenAllNonProviderChecksFailing()
    {
        var checks = new[]
        {
            Entry(HealthCheckKind.ProviderStatus, HealthCheckOutcome.Healthy, HealthCheckClassification.Passing),
            Entry(HealthCheckKind.Ping, HealthCheckOutcome.Failed, HealthCheckClassification.Failing),
            Entry(HealthCheckKind.TcpPort, HealthCheckOutcome.Failed, HealthCheckClassification.Failing),
        };

        Assert.Equal(HealthState.Critical, HealthEvaluationRules.Combine(checks));
    }

    [Fact]
    public void Combine_ReturnsWarning_WhenSomeChecksFailing()
    {
        var checks = new[]
        {
            Entry(HealthCheckKind.ProviderStatus, HealthCheckOutcome.Healthy, HealthCheckClassification.Passing),
            Entry(HealthCheckKind.Ping, HealthCheckOutcome.Healthy, HealthCheckClassification.Passing),
            Entry(HealthCheckKind.TcpPort, HealthCheckOutcome.Failed, HealthCheckClassification.Failing),
        };

        Assert.Equal(HealthState.Warning, HealthEvaluationRules.Combine(checks));
    }

    [Fact]
    public void Combine_ReturnsHealthy_WhenOnlyProviderCheckAndItPasses()
    {
        var checks = new[]
        {
            Entry(HealthCheckKind.ProviderStatus, HealthCheckOutcome.Healthy, HealthCheckClassification.Passing),
        };

        Assert.Equal(HealthState.Healthy, HealthEvaluationRules.Combine(checks));
    }

    [Fact]
    public void Combine_ReturnsUnknown_WhenAllChecksUnsupported()
    {
        var checks = new[]
        {
            Entry(HealthCheckKind.GuestCommand, HealthCheckOutcome.Unsupported, HealthCheckClassification.Passing),
        };

        Assert.Equal(HealthState.Unknown, HealthEvaluationRules.Combine(checks));
    }

    [Fact]
    public void Combine_ReturnsRecovering_WhenCheckIsRecovering()
    {
        var checks = new[]
        {
            Entry(HealthCheckKind.ProviderStatus, HealthCheckOutcome.Healthy, HealthCheckClassification.Passing),
            Entry(HealthCheckKind.Ping, HealthCheckOutcome.Healthy, HealthCheckClassification.Recovering),
        };

        Assert.Equal(HealthState.Recovering, HealthEvaluationRules.Combine(checks));
    }
}

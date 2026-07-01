using FireTower.Shared.Enums;

namespace FireTower.Core.Models;

/// <summary>
/// A single configured health check within a <see cref="HealthProfile"/>.
/// </summary>
public sealed class HealthCheckDefinition
{
    public required string Id { get; init; }
    public required HealthCheckKind Kind { get; init; }
    public bool Enabled { get; init; } = true;
    public int IntervalSeconds { get; init; } = 30;
    public int TimeoutSeconds { get; init; } = 10;
    public int RetryCount { get; init; } = 2;
    public int FailureThreshold { get; init; } = 3;
    public int RecoveryThreshold { get; init; } = 2;
    public IReadOnlyDictionary<string, string> Parameters { get; init; } =
        new Dictionary<string, string>();
}

using FireTower.Shared.Enums;

namespace FireTower.Shared.DTOs;

/// <summary>
/// A single configured health check entry within a Health Profile.
/// </summary>
public sealed class HealthCheckDefinitionDto
{
    public required string Id { get; init; }
    public required HealthCheckKind Kind { get; init; }
    public required bool Enabled { get; init; }
    public required int IntervalSeconds { get; init; }
    public required int TimeoutSeconds { get; init; }
    public required int RetryCount { get; init; }
    public required int FailureThreshold { get; init; }
    public required int RecoveryThreshold { get; init; }
    public IReadOnlyDictionary<string, string> Parameters { get; init; } =
        new Dictionary<string, string>();
}

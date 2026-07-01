using FireTower.Shared.Enums;

namespace FireTower.Core.Models;

/// <summary>
/// The outcome of executing a single health check once.
/// </summary>
public sealed class HealthCheckResult
{
    public required string HealthCheckId { get; init; }
    public required HealthCheckOutcome Outcome { get; init; }
    public required TimeSpan Duration { get; init; }
    public string? Detail { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
}

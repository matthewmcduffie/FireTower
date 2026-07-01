using FireTower.Shared.Enums;

namespace FireTower.Shared.DTOs;

/// <summary>
/// Result of running a single health check on demand via <c>RunHealthCheck</c>.
/// </summary>
public sealed class HealthCheckOutcomeDto
{
    public required string HealthCheckId { get; init; }
    public required HealthCheckOutcome Outcome { get; init; }
    public required TimeSpan Duration { get; init; }
    public string? Detail { get; init; }
}

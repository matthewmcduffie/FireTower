namespace FireTower.Shared.Enums;

/// <summary>
/// Result of a single health check execution, before threshold evaluation combines
/// it into an overall <see cref="HealthState"/>.
/// </summary>
public enum HealthCheckOutcome
{
    Unknown,
    Healthy,
    Warning,
    Failed,
    Timeout,
    Unsupported,
}

using FireTower.Core.Models;
using FireTower.Shared.Enums;

namespace FireTower.Core.Health.Checks;

/// <summary>
/// Shared constructor for <see cref="HealthCheckResult"/> instances, used by every
/// <see cref="Interfaces.IHealthCheck"/> implementation to avoid repeating timestamp handling.
/// </summary>
internal static class HealthCheckResultFactory
{
    public static HealthCheckResult Create(string id, HealthCheckOutcome outcome, TimeSpan duration, string? detail) => new()
    {
        HealthCheckId = id,
        Outcome = outcome,
        Duration = duration,
        Detail = detail,
        Timestamp = DateTimeOffset.UtcNow,
    };
}

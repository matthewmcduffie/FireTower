namespace FireTower.Core.Models;

/// <summary>
/// A reusable set of health checks. Many virtual machines may reference the same profile;
/// changing the profile updates every VM that uses it.
/// </summary>
public sealed class HealthProfile
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public IReadOnlyList<HealthCheckDefinition> Checks { get; init; } =
        Array.Empty<HealthCheckDefinition>();
}

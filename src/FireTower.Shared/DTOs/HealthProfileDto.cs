namespace FireTower.Shared.DTOs;

/// <summary>
/// A reusable set of health checks that may be shared by multiple virtual machines.
/// </summary>
public sealed class HealthProfileDto
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public IReadOnlyList<HealthCheckDefinitionDto> Checks { get; init; } =
        Array.Empty<HealthCheckDefinitionDto>();
}

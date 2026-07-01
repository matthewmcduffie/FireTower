namespace FireTower.Data.Entities;

/// <summary>
/// Flat row shape of the VirtualMachines table, mapped by Dapper.
/// </summary>
public sealed class VirtualMachineEntity
{
    public required string Id { get; init; }
    public required string ExternalId { get; init; }
    public required string Name { get; init; }
    public required string ProviderId { get; init; }
    public required bool Enabled { get; init; }
    public required string HealthProfileId { get; init; }
    public required string RecoveryProfileId { get; init; }
    public required string Tags { get; init; }
    public required string PowerState { get; init; }
    public required string Health { get; init; }
    public required string RecoveryState { get; init; }
    public required int RestartCount { get; init; }
    public DateTimeOffset? LastHealthCheck { get; init; }
    public DateTimeOffset? LastRestart { get; init; }
    public required DateTimeOffset DateCreated { get; init; }
    public required DateTimeOffset DateModified { get; init; }
}

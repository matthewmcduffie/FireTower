namespace FireTower.Data.Entities;

/// <summary>
/// Flat row shape of the HealthHistory table, mapped by Dapper.
/// </summary>
public sealed class HealthHistoryEntity
{
    public long Id { get; init; }
    public required string VirtualMachineId { get; init; }
    public required string PreviousState { get; init; }
    public required string NewState { get; init; }
    public required string CheckResultsJson { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
}

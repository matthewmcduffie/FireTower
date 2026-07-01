namespace FireTower.Data.Entities;

/// <summary>
/// Flat row shape of the Events table, mapped by Dapper.
/// </summary>
public sealed class EventEntity
{
    public long Id { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
    public required string Category { get; init; }
    public required string Message { get; init; }
    public string? VirtualMachineId { get; init; }
    public string? CorrelationId { get; init; }
}

namespace FireTower.Data.Entities;

/// <summary>
/// Flat row shape of the RestartHistory table, mapped by Dapper.
/// </summary>
public sealed class RestartHistoryEntity
{
    public long Id { get; init; }
    public required string VirtualMachineId { get; init; }
    public required string Action { get; init; }
    public required bool Success { get; init; }
    public required string FailureCategory { get; init; }
    public string? FailureReason { get; init; }
    public required long DurationMs { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
    public required string CorrelationId { get; init; }
}

namespace FireTower.Shared.DTOs;

/// <summary>
/// A single structured log entry, as displayed on the tray application's Logs page.
/// </summary>
public sealed class LogEntryDto
{
    public required DateTimeOffset Timestamp { get; init; }
    public required string Level { get; init; }
    public required string Component { get; init; }
    public required string Message { get; init; }
    public string? VirtualMachine { get; init; }
    public string? CorrelationId { get; init; }
    public string? Exception { get; init; }
}

namespace FireTower.Shared.DTOs;

/// <summary>
/// An important operational event, as displayed on the tray application's Events page.
/// </summary>
public sealed class EventRecordDto
{
    public required DateTimeOffset Timestamp { get; init; }
    public required string Category { get; init; }
    public required string Message { get; init; }
    public string? VirtualMachine { get; init; }
    public string? CorrelationId { get; init; }
}

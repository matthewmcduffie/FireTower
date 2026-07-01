namespace FireTower.Core.Models;

/// <summary>
/// A single operational event covering service, provider, discovery, and configuration
/// activity. These share an identical shape, so one table backs all four categories
/// described in database.md (ServiceEvents, ProviderEvents, DiscoveryHistory,
/// ConfigurationHistory), distinguished by <see cref="Category"/>.
/// </summary>
public sealed class OperationalEvent
{
    public required DateTimeOffset Timestamp { get; init; }
    public required string Category { get; init; }
    public required string Message { get; init; }
    public Guid? VirtualMachineId { get; init; }
    public string? CorrelationId { get; init; }
}

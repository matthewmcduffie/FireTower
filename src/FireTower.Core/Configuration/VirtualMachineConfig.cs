namespace FireTower.Core.Configuration;

/// <summary>
/// Configuration entry for one monitored virtual machine.
/// </summary>
public sealed class VirtualMachineConfig
{
    public required Guid Id { get; set; }
    public required string ExternalId { get; set; }
    public required string Name { get; set; }
    public required string ProviderId { get; set; }
    public bool Enabled { get; set; } = true;
    public required string HealthProfileId { get; set; }
    public required string RecoveryProfileId { get; set; }
    public List<string> Tags { get; set; } = new();
    public string? Notes { get; set; }
}

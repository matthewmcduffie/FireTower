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
    /// <summary>Full path to the .vbox file as chosen by the user. Used to locate the
    /// correct VirtualBox configuration directory (VBOX_USER_HOME) without guessing.</summary>
    public string? VBoxFilePath { get; set; }
}

namespace FireTower.Shared.Enums;

/// <summary>
/// A capability a provider may advertise. The UI and Core enable or disable functionality
/// based on which capabilities the active provider supports.
/// </summary>
public enum ProviderCapability
{
    Discovery,
    PowerControl,
    Snapshots,
    HealthStatus,
    GuestInformation,
    GuestCommands,
    GracefulShutdown,
    Suspend,
    Resume,
    Metrics,
    Statistics,
}

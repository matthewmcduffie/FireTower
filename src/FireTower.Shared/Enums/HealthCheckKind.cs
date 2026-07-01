namespace FireTower.Shared.Enums;

/// <summary>
/// Identifies which health check implementation a configured check entry should run.
/// </summary>
public enum HealthCheckKind
{
    ProviderStatus,
    Ping,
    TcpPort,
    GuestCommand,
    ProcessExists,
    CustomCommand,
}

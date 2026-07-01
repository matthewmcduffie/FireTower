namespace FireTower.Shared.Enums;

/// <summary>
/// State of the tray application's Named Pipe connection to the Windows Service.
/// </summary>
public enum ConnectionState
{
    Disconnected,
    Connecting,
    Connected,
    Reconnecting,
    AuthenticationFailed,
    ServerUnavailable,
}

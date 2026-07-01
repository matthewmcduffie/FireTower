namespace FireTower.Shared.Constants;

/// <summary>
/// Default values for the Named Pipe IPC transport. Actual values are sourced from
/// configuration at runtime; these exist only as fallbacks before configuration loads.
/// </summary>
public static class IpcDefaults
{
    public const string PipeName = "FireTower.IPC";
    public const int ProtocolVersion = 1;
    public const int DefaultRequestTimeoutSeconds = 30;
}

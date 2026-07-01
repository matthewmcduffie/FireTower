namespace FireTower.Core.Configuration;

/// <summary>
/// Named Pipe IPC settings loaded from firetower.json.
/// </summary>
public sealed class IpcOptions
{
    public string PipeName { get; set; } = "FireTower.IPC";
    public int RequestTimeoutSeconds { get; set; } = 30;
}

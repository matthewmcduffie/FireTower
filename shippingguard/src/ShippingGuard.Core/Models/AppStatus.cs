namespace ShippingGuard.Core.Models;

public sealed class AppStatus
{
    public required string ProfileId { get; init; }
    public required string DisplayName { get; init; }
    public AppHealthState Health { get; set; } = AppHealthState.Unknown;
    public bool IsRunning { get; set; }
    public bool IsHung { get; set; }
    public int ProcessId { get; set; }
    public int RestartCount { get; set; }
    public DateTimeOffset? LastStarted { get; set; }
    public DateTimeOffset? LastCrashed { get; set; }
    public string? LastAction { get; set; }
    public string? LastError { get; set; }
    public bool MaintenanceMode { get; set; }
    public bool Enabled { get; set; } = true;
}

public enum AppHealthState
{
    Unknown,
    Running,
    Hung,
    Stopped,
    Restarting,
    RetryLimitReached,
    Disabled,
    MaintenanceMode,
}

namespace FireTower.Core.Configuration;

/// <summary>
/// Global application settings loaded from firetower.json.
/// </summary>
public sealed class GlobalOptions
{
    public int ConfigurationVersion { get; set; } = 1;
    public int DefaultPollingIntervalSeconds { get; set; } = 10;
    public string LogLevel { get; set; } = "Information";
    public string ProgramDataPath { get; set; } = @"C:\ProgramData\FireTower";
    public IpcOptions Ipc { get; set; } = new();
    public int ConfigurationBackupCount { get; set; } = 10;
    public int HealthHistoryRetentionDays { get; set; } = 180;
    public int RestartHistoryRetentionDays { get; set; } = 365;
    public int EventRetentionDays { get; set; } = 90;
    public int StatisticsRetentionDays { get; set; } = 365;
    public int StatisticsSnapshotIntervalSeconds { get; set; } = 300;
    public int RetentionCleanupIntervalSeconds { get; set; } = 3600;
}

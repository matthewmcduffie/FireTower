namespace FireTower.Tray.Services;

/// <summary>
/// Tray application preferences persisted to ui.json, per configuration.md.
/// </summary>
public sealed class TrayPreferences
{
    public bool LaunchAtLogin { get; set; } = true;
    public bool MinimizeToTrayOnStartup { get; set; }
    public string Theme { get; set; } = "Dark";
    public int FallbackPollingSeconds { get; set; } = 15;
}

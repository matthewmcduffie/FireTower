namespace FireTower.Tray.Services;

/// <summary>
/// Loads and saves the tray application's own local preferences (ui.json), independent of
/// the service's configuration, which is reached over IPC.
/// </summary>
public interface ITrayPreferencesService
{
    TrayPreferences Current { get; }

    Task LoadAsync();

    Task SaveAsync(TrayPreferences preferences);
}

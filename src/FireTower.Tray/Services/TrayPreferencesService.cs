using System.IO;
using System.Text.Json;

namespace FireTower.Tray.Services;

/// <summary>
/// Default <see cref="ITrayPreferencesService"/> implementation.
/// Preferences are per-user so they are stored in %LocalAppData%\FireTower,
/// not in %ProgramData%\FireTower (which the Windows Service owns and which
/// may not be writable by a standard user before the service has run).
/// </summary>
public sealed class TrayPreferencesService : ITrayPreferencesService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly string _filePath;

    public TrayPreferencesService()
    {
        var localDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "FireTower");
        _filePath = Path.Combine(localDir, "ui.json");
    }

    public TrayPreferences Current { get; private set; } = new();

    public async Task LoadAsync()
    {
        try
        {
            if (!File.Exists(_filePath))
            {
                await SaveAsync(Current).ConfigureAwait(false);
                return;
            }

            await using var stream = File.OpenRead(_filePath);
            Current = await JsonSerializer.DeserializeAsync<TrayPreferences>(stream, JsonOptions)
                          .ConfigureAwait(false)
                      ?? new TrayPreferences();
        }
        catch
        {
            // If the file can't be read or written (e.g. first run, permissions),
            // fall back to defaults so the tray can still open.
            Current = new TrayPreferences();
        }
    }

    public async Task SaveAsync(TrayPreferences preferences)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
        await using var stream = File.Create(_filePath);
        await JsonSerializer.SerializeAsync(stream, preferences, JsonOptions).ConfigureAwait(false);
        Current = preferences;
    }
}

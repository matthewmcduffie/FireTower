using System.IO;
using System.Text.Json;
using FireTower.Core.Configuration.Paths;

namespace FireTower.Tray.Services;

/// <summary>
/// Default <see cref="ITrayPreferencesService"/> implementation, storing ui.json alongside
/// the service's own configuration files.
/// </summary>
public sealed class TrayPreferencesService : ITrayPreferencesService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly string _filePath;

    public TrayPreferencesService(IFireTowerPaths paths)
    {
        _filePath = Path.Combine(paths.ConfigDirectory, "ui.json");
    }

    public TrayPreferences Current { get; private set; } = new();

    public async Task LoadAsync()
    {
        if (!File.Exists(_filePath))
        {
            await SaveAsync(Current).ConfigureAwait(false);
            return;
        }

        await using var stream = File.OpenRead(_filePath);
        Current = await JsonSerializer.DeserializeAsync<TrayPreferences>(stream, JsonOptions).ConfigureAwait(false)
                  ?? new TrayPreferences();
    }

    public async Task SaveAsync(TrayPreferences preferences)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
        await using var stream = File.Create(_filePath);
        await JsonSerializer.SerializeAsync(stream, preferences, JsonOptions).ConfigureAwait(false);
        Current = preferences;
    }
}

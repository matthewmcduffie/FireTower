using System.Text.Json;
using FireTower.Core.Configuration.Paths;
using FireTower.Core.Interfaces;
using FireTower.Core.Models;
using FireTower.Shared.Exceptions;
using Microsoft.Extensions.Logging;

namespace FireTower.Core.Configuration;

/// <summary>
/// Loads, validates, saves, reloads, imports, and exports FireTower's configuration from
/// the files described in configuration.md. This is the only component permitted to write
/// configuration files; everything else goes through <see cref="IConfigurationManager"/>.
/// </summary>
public sealed class ConfigurationManager : IConfigurationManager
{
    private const string GlobalFileName = "firetower.json";
    private const string ProvidersFileName = "providers.json";
    private const string HealthProfilesFileName = "health-profiles.json";
    private const string RecoveryProfilesFileName = "recovery-profiles.json";
    private const string VirtualMachinesFileName = "virtual-machines.json";

    private readonly IFireTowerPaths _paths;
    private readonly ILogger<ConfigurationManager> _logger;
    private readonly SemaphoreSlim _saveLock = new(1, 1);

    private FireTowerConfiguration _current = ConfigurationDefaults.Create();

    public ConfigurationManager(IFireTowerPaths paths, ILogger<ConfigurationManager> logger)
    {
        _paths = paths;
        _logger = logger;
    }

    public FireTowerConfiguration Current => _current;

    public event EventHandler<FireTowerConfiguration>? ConfigurationChanged;

    public async Task LoadAsync(CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(_paths.ConfigDirectory);

        var configuration = new FireTowerConfiguration
        {
            Global = await ReadOrCreateAsync(GlobalFileName, () => new GlobalOptions(), cancellationToken).ConfigureAwait(false),
            Providers = await ReadOrCreateAsync(ProvidersFileName, () => new List<ProviderOptions>(), cancellationToken).ConfigureAwait(false),
            HealthProfiles = await ReadOrCreateAsync(
                HealthProfilesFileName,
                () => new List<HealthProfile> { ConfigurationDefaults.Create().HealthProfiles[0] },
                cancellationToken).ConfigureAwait(false),
            RecoveryProfiles = await ReadOrCreateAsync(
                RecoveryProfilesFileName,
                () => new List<RecoveryProfile> { ConfigurationDefaults.Create().RecoveryProfiles[0] },
                cancellationToken).ConfigureAwait(false),
            VirtualMachines = await ReadOrCreateAsync(VirtualMachinesFileName, () => new List<VirtualMachineConfig>(), cancellationToken).ConfigureAwait(false),
        };

        var errors = ConfigurationValidator.Validate(configuration);
        if (errors.Count > 0)
        {
            throw new ConfigurationException($"Configuration failed validation: {string.Join("; ", errors)}");
        }

        _current = configuration;
        _logger.LogInformation("Configuration loaded from {ConfigDirectory}", _paths.ConfigDirectory);
    }

    public IReadOnlyList<string> Validate(FireTowerConfiguration configuration) =>
        ConfigurationValidator.Validate(configuration);

    public async Task SaveAsync(FireTowerConfiguration configuration, CancellationToken cancellationToken)
    {
        var errors = ConfigurationValidator.Validate(configuration);
        if (errors.Count > 0)
        {
            throw new ValidationException(errors);
        }

        await _saveLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await BackupAsync(cancellationToken).ConfigureAwait(false);

            await WriteAsync(GlobalFileName, configuration.Global, cancellationToken).ConfigureAwait(false);
            await WriteAsync(ProvidersFileName, configuration.Providers, cancellationToken).ConfigureAwait(false);
            await WriteAsync(HealthProfilesFileName, configuration.HealthProfiles, cancellationToken).ConfigureAwait(false);
            await WriteAsync(RecoveryProfilesFileName, configuration.RecoveryProfiles, cancellationToken).ConfigureAwait(false);
            await WriteAsync(VirtualMachinesFileName, configuration.VirtualMachines, cancellationToken).ConfigureAwait(false);

            _current = configuration;
            _logger.LogInformation("Configuration saved");
        }
        finally
        {
            _saveLock.Release();
        }

        ConfigurationChanged?.Invoke(this, configuration);
    }

    public async Task ReloadAsync(CancellationToken cancellationToken)
    {
        await LoadAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Configuration reloaded");
        ConfigurationChanged?.Invoke(this, _current);
    }

    public Task<string> ExportAsync(CancellationToken cancellationToken) =>
        Task.FromResult(JsonSerializer.Serialize(_current, ConfigurationSerialization.Options));

    public async Task ImportAsync(string json, CancellationToken cancellationToken)
    {
        FireTowerConfiguration? imported;
        try
        {
            imported = JsonSerializer.Deserialize<FireTowerConfiguration>(json, ConfigurationSerialization.Options);
        }
        catch (JsonException ex)
        {
            throw new ConfigurationException("Imported configuration is not valid JSON.", ex);
        }

        if (imported is null)
        {
            throw new ConfigurationException("Imported configuration was empty.");
        }

        await SaveAsync(imported, cancellationToken).ConfigureAwait(false);
    }

    private async Task<T> ReadOrCreateAsync<T>(string fileName, Func<T> createDefault, CancellationToken cancellationToken)
    {
        var path = Path.Combine(_paths.ConfigDirectory, fileName);
        if (!File.Exists(path))
        {
            var defaultValue = createDefault();
            await WriteAsync(fileName, defaultValue, cancellationToken).ConfigureAwait(false);
            return defaultValue;
        }

        await using var stream = File.OpenRead(path);
        try
        {
            var value = await JsonSerializer.DeserializeAsync<T>(stream, ConfigurationSerialization.Options, cancellationToken).ConfigureAwait(false);
            if (value is null)
            {
                throw new ConfigurationException($"{fileName} deserialized to null.");
            }

            return value;
        }
        catch (JsonException ex)
        {
            throw new ConfigurationException($"{fileName} is not valid JSON.", ex);
        }
    }

    private async Task WriteAsync<T>(string fileName, T value, CancellationToken cancellationToken)
    {
        var path = Path.Combine(_paths.ConfigDirectory, fileName);
        var tempPath = path + ".tmp";

        await using (var stream = File.Create(tempPath))
        {
            await JsonSerializer.SerializeAsync(stream, value, ConfigurationSerialization.Options, cancellationToken).ConfigureAwait(false);
        }

        File.Move(tempPath, path, overwrite: true);
    }

    private async Task BackupAsync(CancellationToken cancellationToken)
    {
        if (!Directory.Exists(_paths.ConfigDirectory))
        {
            return;
        }

        Directory.CreateDirectory(_paths.BackupsDirectory);
        var timestamp = DateTimeOffset.UtcNow.ToString("yyyyMMddHHmmss");
        var backupDirectory = Path.Combine(_paths.BackupsDirectory, $"config-{timestamp}");
        Directory.CreateDirectory(backupDirectory);

        foreach (var file in Directory.EnumerateFiles(_paths.ConfigDirectory, "*.json"))
        {
            var destination = Path.Combine(backupDirectory, Path.GetFileName(file));
            File.Copy(file, destination, overwrite: true);
        }

        await PruneOldBackupsAsync(cancellationToken).ConfigureAwait(false);
    }

    private Task PruneOldBackupsAsync(CancellationToken cancellationToken)
    {
        var keep = Math.Max(1, _current.Global.ConfigurationBackupCount);
        var backups = Directory.GetDirectories(_paths.BackupsDirectory, "config-*")
            .OrderByDescending(d => d)
            .Skip(keep);

        foreach (var stale in backups)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Directory.Delete(stale, recursive: true);
        }

        return Task.CompletedTask;
    }
}

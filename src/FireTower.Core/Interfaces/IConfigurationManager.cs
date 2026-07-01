using FireTower.Core.Configuration;

namespace FireTower.Core.Interfaces;

/// <summary>
/// Loads, validates, saves, reloads, imports, exports, and backs up FireTower's configuration.
/// No other subsystem may modify configuration files directly (configuration.md).
/// </summary>
public interface IConfigurationManager
{
    FireTowerConfiguration Current { get; }

    Task LoadAsync(CancellationToken cancellationToken);

    IReadOnlyList<string> Validate(FireTowerConfiguration configuration);

    Task SaveAsync(FireTowerConfiguration configuration, CancellationToken cancellationToken);

    Task ReloadAsync(CancellationToken cancellationToken);

    Task<string> ExportAsync(CancellationToken cancellationToken);

    Task ImportAsync(string json, CancellationToken cancellationToken);

    event EventHandler<FireTowerConfiguration>? ConfigurationChanged;
}

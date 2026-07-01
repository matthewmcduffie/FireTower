namespace FireTower.Core.Configuration.Paths;

/// <summary>
/// Resolves the on-disk locations FireTower uses for configuration, logs, data, and backups.
/// All paths derive from a single configurable root so nothing is hardcoded into binaries.
/// </summary>
public interface IFireTowerPaths
{
    string RootDirectory { get; }
    string ConfigDirectory { get; }
    string LogsDirectory { get; }
    string DataDirectory { get; }
    string BackupsDirectory { get; }
}

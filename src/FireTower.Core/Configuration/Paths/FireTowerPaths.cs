namespace FireTower.Core.Configuration.Paths;

/// <summary>
/// Default <see cref="IFireTowerPaths"/> implementation. The root directory is
/// %ProgramData%\FireTower unless overridden by the FIRETOWER_DATA_DIR environment
/// variable, which keeps the location configurable without recompiling or hardcoding
/// a drive letter.
/// </summary>
public sealed class FireTowerPaths : IFireTowerPaths
{
    private const string OverrideEnvironmentVariable = "FIRETOWER_DATA_DIR";

    public string RootDirectory { get; }
    public string ConfigDirectory { get; }
    public string LogsDirectory { get; }
    public string DataDirectory { get; }
    public string BackupsDirectory { get; }

    public FireTowerPaths()
    {
        var overridePath = Environment.GetEnvironmentVariable(OverrideEnvironmentVariable);
        RootDirectory = !string.IsNullOrWhiteSpace(overridePath)
            ? overridePath
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "FireTower");

        ConfigDirectory = Path.Combine(RootDirectory, "config");
        LogsDirectory = Path.Combine(RootDirectory, "logs");
        DataDirectory = Path.Combine(RootDirectory, "data");
        BackupsDirectory = Path.Combine(RootDirectory, "backups");
    }

    public void EnsureDirectoriesExist()
    {
        Directory.CreateDirectory(ConfigDirectory);
        Directory.CreateDirectory(LogsDirectory);
        Directory.CreateDirectory(DataDirectory);
        Directory.CreateDirectory(BackupsDirectory);
    }
}

namespace ShippingGuard.Core.Configuration;

public sealed class AgentSettings
{
    public string ProfilesDirectory { get; set; } = DefaultProfilesDirectory;
    public string LogsDirectory { get; set; } = DefaultLogsDirectory;
    public string PipeName { get; set; } = "ShippingGuard.IPC";

    public static readonly string DefaultRoot =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "ShippingGuard");

    public static readonly string DefaultProfilesDirectory =
        Path.Combine(DefaultRoot, "profiles");

    public static readonly string DefaultLogsDirectory =
        Path.Combine(DefaultRoot, "logs");

    public void EnsureDirectoriesExist()
    {
        Directory.CreateDirectory(ProfilesDirectory);
        Directory.CreateDirectory(LogsDirectory);
    }
}

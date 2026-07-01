namespace FireTower.Providers.VirtualBox.Services;

/// <summary>
/// Resolves the path to VBoxManage.exe without ever hardcoding it, per virtualbox.md.
/// </summary>
public interface IVBoxManageLocator
{
    /// <summary>
    /// Resolves the VBoxManage executable path. Tries, in order: the configured path,
    /// the VirtualBox registry installation key, then the standard installation directory.
    /// Throws if none of those locations contain a valid executable.
    /// </summary>
    string Locate(string? configuredPath);
}

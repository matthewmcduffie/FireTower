using System.Runtime.Versioning;
using FireTower.Shared.Exceptions;
using Microsoft.Win32;

namespace FireTower.Providers.VirtualBox.Services;

/// <summary>
/// Default <see cref="IVBoxManageLocator"/> implementation.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class VBoxManageLocator : IVBoxManageLocator
{
    private const string ExecutableName = "VBoxManage.exe";
    private const string RegistryKeyPath = @"SOFTWARE\Oracle\VirtualBox";

    public string Locate(string? configuredPath)
    {
        if (!string.IsNullOrWhiteSpace(configuredPath) && File.Exists(configuredPath))
        {
            return configuredPath;
        }

        var registryPath = LocateViaRegistry();
        if (registryPath is not null)
        {
            return registryPath;
        }

        var standardPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            "Oracle", "VirtualBox", ExecutableName);

        if (File.Exists(standardPath))
        {
            return standardPath;
        }

        throw new ProviderException(
            "virtualbox",
            "VBoxManage.exe could not be located. Configure 'executablePath' for the VirtualBox provider in providers.json.");
    }

    private static string? LocateViaRegistry()
    {
        using var key = Registry.LocalMachine.OpenSubKey(RegistryKeyPath);
        var installDir = key?.GetValue("InstallDir") as string;
        if (string.IsNullOrWhiteSpace(installDir))
        {
            return null;
        }

        var candidate = Path.Combine(installDir, ExecutableName);
        return File.Exists(candidate) ? candidate : null;
    }
}

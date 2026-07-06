namespace FireTower.Providers.VirtualBox.Commands;

/// <summary>
/// Executes VBoxManage with a fixed set of arguments and returns a structured result.
/// Kept independent of discovery/parsing so it can be mocked in provider tests without
/// a real VirtualBox installation.
/// </summary>
public interface IVBoxCommandRunner
{
    Task<VBoxCommandResult> RunAsync(IReadOnlyList<string> arguments, TimeSpan timeout, CancellationToken cancellationToken);

    /// <summary>
    /// Probes user profile directories to find the one that makes VBoxManage work.
    /// Called once during provider initialization; result is cached for all subsequent calls.
    /// </summary>
    Task CalibrateVBoxUserHomeAsync(TimeSpan timeout, CancellationToken cancellationToken);
}

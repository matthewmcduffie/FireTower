namespace ShippingGuard.Core.Models;

/// <summary>
/// Defines a single application that ShippingGuard monitors.
/// Loaded from a JSON file in the profiles directory.
/// </summary>
public sealed class AppProfile
{
    public required string Id { get; init; }
    public required string DisplayName { get; init; }
    public required string ProcessName { get; init; }

    /// <summary>
    /// Full launch command including arguments, e.g. "C:\UPS\WorldShip.exe" /silent
    /// If set, takes precedence over ExecutablePath + Arguments.
    /// </summary>
    public string? LaunchCommand { get; init; }

    public string? ExecutablePath { get; init; }
    public string? Arguments { get; init; }

    public int StartupDelaySeconds { get; init; } = 5;
    public int CheckIntervalSeconds { get; init; } = 10;
    public bool RestartIfMissing { get; init; } = true;
    public bool KillIfHung { get; init; } = true;
    public int HungTimeoutSeconds { get; init; } = 30;
    public int MaxRestartAttempts { get; init; } = 5;
    public int RestartCooldownSeconds { get; init; } = 30;
    public bool Enabled { get; init; } = true;

    public IReadOnlyList<DialogRule> DialogRules { get; init; } = Array.Empty<DialogRule>();

    /// <summary>
    /// Resolves the effective launch command from LaunchCommand or ExecutablePath + Arguments.
    /// </summary>
    public (string Exe, string Args) ResolveCommand()
    {
        if (!string.IsNullOrWhiteSpace(LaunchCommand))
        {
            var parts = SplitCommand(LaunchCommand);
            return (parts.Exe, parts.Args);
        }
        return (ExecutablePath ?? string.Empty, Arguments ?? string.Empty);
    }

    private static (string Exe, string Args) SplitCommand(string command)
    {
        command = command.Trim();
        if (command.StartsWith('"'))
        {
            var close = command.IndexOf('"', 1);
            if (close > 0)
            {
                return (command[1..close], command[(close + 1)..].Trim());
            }
        }
        var space = command.IndexOf(' ');
        return space < 0 ? (command, string.Empty) : (command[..space], command[(space + 1)..].Trim());
    }
}

namespace FireTower.Providers.VirtualBox.Commands;

/// <summary>
/// The structured result of a single VBoxManage invocation: exit code, captured output,
/// and timing, per the Command Execution requirements in virtualbox.md.
/// </summary>
public sealed record VBoxCommandResult(int ExitCode, string StandardOutput, string StandardError, TimeSpan Duration)
{
    public bool Succeeded => ExitCode == 0;
}

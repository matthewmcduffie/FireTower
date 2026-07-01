namespace FireTower.Core.Configuration;

/// <summary>
/// Per-provider configuration loaded from providers.json. Provider-specific values that
/// don't fit this common shape live in <see cref="ExtraSettings"/>.
/// </summary>
public sealed class ProviderOptions
{
    public required string ProviderId { get; set; }
    public bool Enabled { get; set; } = true;
    public string? ExecutablePath { get; set; }
    public int OperationTimeoutSeconds { get; set; } = 30;
    public int DiscoveryIntervalSeconds { get; set; } = 300;
    public int RetryCount { get; set; } = 2;
    public int RetryDelaySeconds { get; set; } = 5;
    public int MaxConcurrentOperations { get; set; } = 4;

    /// <summary>
    /// VirtualBox start type passed to <c>VBoxManage startvm --type</c>.
    /// Valid values: <c>gui</c> (default — shows the VM window), <c>headless</c>
    /// (no display, background only), <c>sdl</c>, <c>separate</c>.
    /// Change to "headless" in providers.json for server VMs that need no display.
    /// </summary>
    public string VmStartType { get; set; } = "gui";

    public Dictionary<string, string> ExtraSettings { get; set; } = new();
}

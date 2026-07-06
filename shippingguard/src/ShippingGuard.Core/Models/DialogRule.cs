namespace ShippingGuard.Core.Models;

/// <summary>
/// Defines how to handle a specific dialog that may appear while an app is running.
/// Matched by window title or visible text; action is click a named button.
/// </summary>
public sealed class DialogRule
{
    public required string Name { get; init; }
    public string? TitleContains { get; init; }
    public string? TextContains { get; init; }
    public required string ButtonText { get; init; }
    public DialogAction Action { get; init; } = DialogAction.Click;
    public bool Enabled { get; init; } = true;

    /// <summary>
    /// When true this rule only fires while maintenance mode is active.
    /// Use this for update dialogs you only want handled during deliberate maintenance.
    /// </summary>
    public bool MaintenanceModeOnly { get; init; }
    public string? Notes { get; init; }
}

public enum DialogAction
{
    Click,
    Dismiss,
    Log,
}

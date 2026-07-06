using System.Windows.Automation;
using Microsoft.Extensions.Logging;
using ShippingGuard.Core.Models;

namespace ShippingGuard.UiAutomation;

/// <summary>
/// Scans for dialog windows belonging to monitored processes and applies
/// configured dialog rules. Never uses screen coordinates — all matching is
/// done through the Windows UI Automation tree.
/// </summary>
public sealed class DialogWatcher
{
    private readonly ILogger<DialogWatcher> _logger;

    public DialogWatcher(ILogger<DialogWatcher> logger)
    {
        _logger = logger;
    }

    public DialogResult? CheckAndHandle(AppProfile profile, bool maintenanceMode)
    {
        try
        {
            var windows = FindDialogWindows(profile.ProcessName);
            foreach (var window in windows)
            {
                var result = TryHandleWindow(window, profile, maintenanceMode);
                if (result is not null) return result;
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "[{App}] UI Automation scan error.", profile.DisplayName);
        }
        return null;
    }

    private IEnumerable<AutomationElement> FindDialogWindows(string processName)
    {
        var condition = new AndCondition(
            new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Window),
            new PropertyCondition(AutomationElement.ProcessIdProperty,
                GetProcessId(processName)));

        return AutomationElement.RootElement
            .FindAll(TreeScope.Children, condition)
            .Cast<AutomationElement>();
    }

    private int GetProcessId(string processName)
    {
        var procs = System.Diagnostics.Process.GetProcessesByName(processName);
        return procs.FirstOrDefault()?.Id ?? 0;
    }

    private DialogResult? TryHandleWindow(AutomationElement window, AppProfile profile, bool maintenanceMode)
    {
        var title = window.Current.Name ?? string.Empty;
        var text  = GetWindowText(window);

        foreach (var rule in profile.DialogRules.Where(r => r.Enabled))
        {
            if (rule.MaintenanceModeOnly && !maintenanceMode) continue;

            bool matches =
                (string.IsNullOrEmpty(rule.TitleContains) || title.Contains(rule.TitleContains, StringComparison.OrdinalIgnoreCase)) &&
                (string.IsNullOrEmpty(rule.TextContains)  || text.Contains(rule.TextContains,   StringComparison.OrdinalIgnoreCase));

            if (!matches) continue;

            _logger.LogInformation("[{App}] Dialog matched rule '{Rule}': title='{Title}'",
                profile.DisplayName, rule.Name, title);

            if (rule.Action == DialogAction.Log)
                return new DialogResult(rule.Name, title, ButtonClicked: null, Acted: false);

            if (TryClickButton(window, rule.ButtonText, profile.DisplayName, rule.Name))
                return new DialogResult(rule.Name, title, rule.ButtonText, Acted: true);
        }

        // Unknown dialog — log and alert, do not click
        if (IsDialog(window))
        {
            _logger.LogWarning("[{App}] Unknown dialog: '{Title}' — not clicking.", profile.DisplayName, title);
            return new DialogResult("Unknown", title, ButtonClicked: null, Acted: false, IsUnknown: true);
        }

        return null;
    }

    private static bool IsDialog(AutomationElement window)
    {
        // A dialog typically has no minimize button and has a parent process window
        var style = (int)window.GetCurrentPropertyValue(AutomationElement.NativeWindowHandleProperty);
        return style != 0;
    }

    private bool TryClickButton(AutomationElement window, string buttonText, string appName, string ruleName)
    {
        var buttons = window.FindAll(TreeScope.Descendants,
            new AndCondition(
                new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Button),
                new PropertyCondition(AutomationElement.NameProperty, buttonText)));

        if (buttons.Count == 0)
        {
            _logger.LogWarning("[{App}] Rule '{Rule}': button '{Button}' not found.", appName, ruleName, buttonText);
            return false;
        }

        var button = buttons.Cast<AutomationElement>().First();
        if (button.TryGetCurrentPattern(InvokePattern.Pattern, out var pattern) &&
            pattern is InvokePattern invoke)
        {
            invoke.Invoke();
            _logger.LogInformation("[{App}] Rule '{Rule}': clicked '{Button}'.", appName, ruleName, buttonText);
            return true;
        }

        _logger.LogWarning("[{App}] Rule '{Rule}': button '{Button}' found but not invokable.", appName, ruleName, buttonText);
        return false;
    }

    private static string GetWindowText(AutomationElement window)
    {
        var texts = window.FindAll(TreeScope.Descendants,
            new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Text));
        return string.Join(" ", texts.Cast<AutomationElement>().Select(e => e.Current.Name));
    }
}

public sealed record DialogResult(string RuleName, string WindowTitle,
    string? ButtonClicked, bool Acted, bool IsUnknown = false);

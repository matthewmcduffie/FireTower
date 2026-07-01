using CommunityToolkit.Mvvm.ComponentModel;

namespace FireTower.Tray.ViewModels;

/// <summary>
/// Common state shared by every page ViewModel: a busy flag, a user-facing error message,
/// and a transient success status message. Both messages are surfaced by the main window
/// so individual views don't each need their own notification area.
/// </summary>
public abstract partial class ViewModelBase : ObservableObject
{
    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private string? _statusMessage;

    /// <summary>
    /// Sets <see cref="StatusMessage"/> and schedules automatic clearing after the
    /// specified delay, so temporary "Success" banners disappear without user action.
    /// </summary>
    protected void ShowStatus(string message, TimeSpan? clearAfter = null)
    {
        StatusMessage = message;
        ErrorMessage = null;
        var delay = clearAfter ?? TimeSpan.FromSeconds(4);
        _ = ClearStatusAfterAsync(delay);
    }

    protected void ShowError(string message)
    {
        ErrorMessage = message;
        StatusMessage = null;
    }

    private async Task ClearStatusAfterAsync(TimeSpan delay)
    {
        await Task.Delay(delay);
        if (StatusMessage != null)
        {
            StatusMessage = null;
        }
    }
}

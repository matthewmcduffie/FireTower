namespace FireTower.Tray.Services;

/// <summary>
/// Marshals work onto the WPF UI thread. IPC events arrive on background threads (per
/// ipc.md's independent-connection model); any ViewModel state they touch must be updated
/// through this interface rather than directly, or WPF data binding will fail silently.
/// </summary>
public interface IUiDispatcher
{
    void Invoke(Action action);
}

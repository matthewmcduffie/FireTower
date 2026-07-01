using System.Windows.Threading;

namespace FireTower.Tray.Services;

/// <summary>
/// Default <see cref="IUiDispatcher"/> implementation backed by the application's
/// <see cref="Dispatcher"/>.
/// </summary>
public sealed class WpfDispatcher : IUiDispatcher
{
    public void Invoke(Action action)
    {
        var dispatcher = System.Windows.Application.Current?.Dispatcher;
        if (dispatcher is null || dispatcher.CheckAccess())
        {
            action();
        }
        else
        {
            // Fire-and-forget by design: this marshals a property update onto the UI
            // thread from a background IPC callback; nothing awaits the UI update itself.
            _ = dispatcher.BeginInvoke(action);
        }
    }
}

namespace ShippingGuard.Notifications;

/// <summary>
/// Fires Windows balloon-tip / toast notifications via the tray icon.
/// The tray app subscribes to the NotificationRequested event and forwards
/// the alert to Hardcodet.Wpf.TaskbarNotification's ShowBalloonTip method.
/// </summary>
public sealed class BalloonNotificationService : INotificationService
{
    public event EventHandler<NotificationEventArgs>? NotificationRequested;

    public void Notify(string title, string message, NotificationLevel level = NotificationLevel.Info)
    {
        NotificationRequested?.Invoke(this, new NotificationEventArgs(title, message, level));
    }
}

public sealed record NotificationEventArgs(string Title, string Message, NotificationLevel Level);

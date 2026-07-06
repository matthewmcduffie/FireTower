namespace ShippingGuard.Notifications;

public interface INotificationService
{
    void Notify(string title, string message, NotificationLevel level = NotificationLevel.Info);
}

public enum NotificationLevel { Info, Warning, Error }

/// <summary>Placeholder for future email delivery.</summary>
public interface IEmailNotificationService
{
    Task SendAsync(string subject, string body, CancellationToken ct = default);
}

/// <summary>Placeholder for future webhook delivery.</summary>
public interface IWebhookNotificationService
{
    Task PostAsync(string payload, CancellationToken ct = default);
}

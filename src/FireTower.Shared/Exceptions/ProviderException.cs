namespace FireTower.Shared.Exceptions;

/// <summary>
/// Raised by a provider implementation and translated from platform-specific failures.
/// Providers must never let native exceptions (e.g. VirtualBox COM errors) escape their
/// own project; everything else in FireTower sees only this type.
/// </summary>
public sealed class ProviderException : FireTowerException
{
    public string ProviderId { get; }

    public ProviderException(string providerId, string message) : base(message)
    {
        ProviderId = providerId;
    }

    public ProviderException(string providerId, string message, Exception innerException)
        : base(message, innerException)
    {
        ProviderId = providerId;
    }
}

namespace FireTower.Shared.Exceptions;

/// <summary>
/// Raised when configuration fails to load, parse, validate, or upgrade.
/// </summary>
public sealed class ConfigurationException : FireTowerException
{
    public ConfigurationException(string message) : base(message)
    {
    }

    public ConfigurationException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

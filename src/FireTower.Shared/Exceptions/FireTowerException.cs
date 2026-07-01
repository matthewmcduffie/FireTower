namespace FireTower.Shared.Exceptions;

/// <summary>
/// Base type for every exception raised intentionally by FireTower code. Catch this type
/// at component boundaries to distinguish expected failure modes from unexpected bugs.
/// </summary>
public abstract class FireTowerException : Exception
{
    protected FireTowerException(string message) : base(message)
    {
    }

    protected FireTowerException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

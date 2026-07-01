namespace FireTower.Shared.Exceptions;

/// <summary>
/// Raised when a request or configuration value fails validation. Carries enough detail
/// for the caller to explain exactly what is wrong, per the validation requirements in ipc.md
/// and configuration.md.
/// </summary>
public sealed class ValidationException : FireTowerException
{
    public IReadOnlyList<string> Errors { get; }

    public ValidationException(string message) : base(message)
    {
        Errors = new[] { message };
    }

    public ValidationException(IReadOnlyList<string> errors)
        : base(string.Join("; ", errors))
    {
        Errors = errors;
    }
}

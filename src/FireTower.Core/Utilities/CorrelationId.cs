namespace FireTower.Core.Utilities;

/// <summary>
/// Generates correlation identifiers for long-running operations (restart sequences,
/// discovery, configuration reloads) so every log entry tied to one operation can be
/// grouped together, per logging.md.
/// </summary>
public static class CorrelationId
{
    public static string New() => Guid.NewGuid().ToString("N");
}

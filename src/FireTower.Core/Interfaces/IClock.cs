namespace FireTower.Core.Interfaces;

/// <summary>
/// Abstraction over the current time so scheduling, cooldowns, and thresholds can be
/// unit tested deterministically instead of depending on <see cref="DateTimeOffset.UtcNow"/>.
/// </summary>
public interface IClock
{
    DateTimeOffset UtcNow { get; }
}

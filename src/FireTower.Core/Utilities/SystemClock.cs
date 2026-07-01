using FireTower.Core.Interfaces;

namespace FireTower.Core.Utilities;

/// <summary>
/// Default <see cref="IClock"/> implementation backed by the system clock.
/// </summary>
public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}

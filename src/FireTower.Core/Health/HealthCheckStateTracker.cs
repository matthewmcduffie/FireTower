using System.Collections.Concurrent;

namespace FireTower.Core.Health;

/// <summary>
/// Thread-safe store of <see cref="ConsecutiveCheckState"/> per (virtual machine, health
/// check) pair, kept in memory for the lifetime of the service.
/// </summary>
public sealed class HealthCheckStateTracker
{
    private readonly ConcurrentDictionary<(Guid VmId, string CheckId), ConsecutiveCheckState> _states = new();

    public ConsecutiveCheckState GetState(Guid vmId, string checkId) =>
        _states.GetOrAdd((vmId, checkId), static _ => new ConsecutiveCheckState());
}

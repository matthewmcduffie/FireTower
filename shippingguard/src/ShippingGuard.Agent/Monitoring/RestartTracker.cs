using System.Collections.Concurrent;

namespace ShippingGuard.Agent.Monitoring;

public sealed class RestartTracker
{
    private readonly ConcurrentDictionary<string, (int Count, DateTimeOffset WindowStart)> _state = new();

    public int GetCount(string profileId, int windowSeconds)
    {
        if (!_state.TryGetValue(profileId, out var entry)) return 0;
        if ((DateTimeOffset.UtcNow - entry.WindowStart).TotalSeconds > windowSeconds)
        {
            _state.TryRemove(profileId, out _);
            return 0;
        }
        return entry.Count;
    }

    public void Record(string profileId)
    {
        _state.AddOrUpdate(profileId,
            _ => (1, DateTimeOffset.UtcNow),
            (_, existing) => (existing.Count + 1, existing.WindowStart));
    }

    public void Reset(string profileId) => _state.TryRemove(profileId, out _);
}

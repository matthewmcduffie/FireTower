using FireTower.Core.Models;

namespace FireTower.Core.Interfaces;

/// <summary>
/// Persists operational events (service, provider, discovery, configuration) for the tray
/// application's Events page. Implemented by FireTower.Data.
/// </summary>
public interface IEventRepository
{
    Task RecordAsync(OperationalEvent operationalEvent, CancellationToken cancellationToken);

    Task<IReadOnlyList<OperationalEvent>> GetRecentAsync(int skip, int take, CancellationToken cancellationToken);

    Task PurgeOlderThanAsync(DateTimeOffset cutoff, CancellationToken cancellationToken);
}

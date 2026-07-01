using FireTower.Core.Models;

namespace FireTower.Core.Interfaces;

/// <summary>
/// Persists periodic statistics snapshots. Implemented by FireTower.Data.
/// </summary>
public interface IStatisticsRepository
{
    Task RecordAsync(StatisticsSnapshot snapshot, CancellationToken cancellationToken);

    Task<StatisticsSnapshot?> GetLatestAsync(CancellationToken cancellationToken);

    Task PurgeOlderThanAsync(DateTimeOffset cutoff, CancellationToken cancellationToken);
}

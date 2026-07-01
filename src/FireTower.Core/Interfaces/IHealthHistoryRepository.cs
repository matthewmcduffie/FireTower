using FireTower.Core.Models;

namespace FireTower.Core.Interfaces;

/// <summary>
/// Records health state transitions for trend analysis and troubleshooting.
/// Implemented by FireTower.Data.
/// </summary>
public interface IHealthHistoryRepository
{
    Task RecordAsync(HealthEvaluation evaluation, CancellationToken cancellationToken);

    Task<IReadOnlyList<HealthEvaluation>> GetRecentAsync(Guid virtualMachineId, int take, CancellationToken cancellationToken);

    Task PurgeOlderThanAsync(DateTimeOffset cutoff, CancellationToken cancellationToken);
}

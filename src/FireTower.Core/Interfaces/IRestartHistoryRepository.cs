using FireTower.Core.Models;

namespace FireTower.Core.Interfaces;

/// <summary>
/// Records every recovery action taken by the Restart Engine. Entries are never modified
/// after insertion. Implemented by FireTower.Data.
/// </summary>
public interface IRestartHistoryRepository
{
    Task RecordAsync(RecoveryResult result, CancellationToken cancellationToken);

    Task<IReadOnlyList<RecoveryResult>> GetRecentAsync(Guid virtualMachineId, int take, CancellationToken cancellationToken);

    Task<int> CountWithinWindowAsync(Guid virtualMachineId, DateTimeOffset since, CancellationToken cancellationToken);

    Task PurgeOlderThanAsync(DateTimeOffset cutoff, CancellationToken cancellationToken);
}

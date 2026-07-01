using FireTower.Core.Models;

namespace FireTower.Core.Interfaces;

/// <summary>
/// Persists FireTower's view of monitored virtual machines. Implemented by FireTower.Data.
/// </summary>
public interface IVirtualMachineRepository
{
    Task<IReadOnlyList<VirtualMachine>> GetAllAsync(CancellationToken cancellationToken);

    Task<VirtualMachine?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task UpsertAsync(VirtualMachine virtualMachine, CancellationToken cancellationToken);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
}

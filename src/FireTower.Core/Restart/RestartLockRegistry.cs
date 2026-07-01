using System.Collections.Concurrent;

namespace FireTower.Core.Restart;

/// <summary>
/// Enforces the safety rule in restart-engine.md that recovery actions for the same VM
/// must never overlap, while allowing different VMs to recover concurrently.
/// </summary>
public sealed class RestartLockRegistry
{
    private readonly ConcurrentDictionary<Guid, SemaphoreSlim> _locks = new();

    public async Task<IDisposable> AcquireAsync(Guid virtualMachineId, CancellationToken cancellationToken)
    {
        var semaphore = _locks.GetOrAdd(virtualMachineId, static _ => new SemaphoreSlim(1, 1));
        await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        return new Releaser(semaphore);
    }

    private sealed class Releaser : IDisposable
    {
        private readonly SemaphoreSlim _semaphore;
        private bool _disposed;

        public Releaser(SemaphoreSlim semaphore)
        {
            _semaphore = semaphore;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _semaphore.Release();
        }
    }
}

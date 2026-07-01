using FireTower.Core.Models;

namespace FireTower.Core.Interfaces;

/// <summary>
/// Loads, registers, and routes requests to <see cref="IVmProvider"/> instances. This is the
/// only component besides the providers themselves that knows providers exist; every other
/// subsystem goes through this interface.
/// </summary>
public interface IProviderManager
{
    Task InitializeAsync(CancellationToken cancellationToken);

    IReadOnlyList<ProviderRegistration> GetRegisteredProviders();

    IVmProvider GetProvider(string providerId);

    bool TryGetProvider(string providerId, out IVmProvider? provider);
}

using FireTower.Core.Interfaces;
using FireTower.Core.Models;
using FireTower.Shared.Exceptions;
using Microsoft.Extensions.Logging;

namespace FireTower.Core.Providers;

/// <summary>
/// Loads and registers every <see cref="IVmProvider"/> known to the dependency injection
/// container, following the lifecycle in providers.md. A provider that fails to initialize
/// is marked unavailable rather than allowed to crash the service; unrelated providers
/// continue operating normally.
/// </summary>
public sealed class ProviderManager : IProviderManager
{
    private readonly IReadOnlyList<IVmProvider> _allProviders;
    private readonly IConfigurationManager _configurationManager;
    private readonly ILogger<ProviderManager> _logger;

    private readonly Dictionary<string, IVmProvider> _providers = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, ProviderRegistration> _registrations = new(StringComparer.OrdinalIgnoreCase);

    public ProviderManager(IEnumerable<IVmProvider> allProviders, IConfigurationManager configurationManager, ILogger<ProviderManager> logger)
    {
        _allProviders = allProviders.ToList();
        _configurationManager = configurationManager;
        _logger = logger;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        foreach (var provider in _allProviders)
        {
            var options = _configurationManager.Current.Providers
                .FirstOrDefault(p => string.Equals(p.ProviderId, provider.ProviderId, StringComparison.OrdinalIgnoreCase));

            if (options is { Enabled: false })
            {
                _logger.LogInformation("Provider {ProviderId} is disabled by configuration", provider.ProviderId);
                continue;
            }

            try
            {
                await provider.InitializeAsync(cancellationToken).ConfigureAwait(false);
                var registration = await provider.GetCapabilitiesAsync(cancellationToken).ConfigureAwait(false);
                registration.IsAvailable = true;

                _providers[provider.ProviderId] = provider;
                _registrations[provider.ProviderId] = registration;

                _logger.LogInformation("Provider {ProviderId} initialized ({Capabilities})",
                    provider.ProviderId, string.Join(", ", registration.Capabilities));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Provider {ProviderId} failed to initialize and has been disabled for this session", provider.ProviderId);
                _registrations[provider.ProviderId] = new ProviderRegistration
                {
                    ProviderId = provider.ProviderId,
                    FriendlyName = provider.FriendlyName,
                    Version = "unknown",
                    IsAvailable = false,
                    UnavailableReason = ex.Message,
                };
            }
        }
    }

    public IReadOnlyList<ProviderRegistration> GetRegisteredProviders() => _registrations.Values.ToList();

    public IVmProvider GetProvider(string providerId)
    {
        if (!TryGetProvider(providerId, out var provider) || provider is null)
        {
            throw new ProviderException(providerId, $"Provider '{providerId}' is not registered or is unavailable.");
        }

        return provider;
    }

    public bool TryGetProvider(string providerId, out IVmProvider? provider) =>
        _providers.TryGetValue(providerId, out provider);
}

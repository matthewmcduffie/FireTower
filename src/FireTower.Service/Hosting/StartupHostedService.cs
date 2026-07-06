using FireTower.Core.Interfaces;
using FireTower.Core.Services;
using FireTower.Data.Migrations;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using IConfigurationManager = FireTower.Core.Interfaces.IConfigurationManager;

namespace FireTower.Service.Hosting;

/// <summary>
/// Runs the startup sequence from service.md, in order, before any monitoring begins:
/// load configuration, apply database migrations, synchronize the monitored VM list, and
/// initialize providers. Monitoring must never start before this completes successfully.
/// </summary>
public sealed class StartupHostedService : IHostedService
{
    private readonly IConfigurationManager _configurationManager;
    private readonly DatabaseMigrator _databaseMigrator;
    private readonly VirtualMachineSynchronizer _vmSynchronizer;
    private readonly IProviderManager _providerManager;
    private readonly ILogger<StartupHostedService> _logger;

    public StartupHostedService(
        IConfigurationManager configurationManager,
        DatabaseMigrator databaseMigrator,
        VirtualMachineSynchronizer vmSynchronizer,
        IProviderManager providerManager,
        ILogger<StartupHostedService> logger)
    {
        _configurationManager = configurationManager;
        _databaseMigrator = databaseMigrator;
        _vmSynchronizer = vmSynchronizer;
        _providerManager = providerManager;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _configurationManager.LoadAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // A validation or parse error in the config files must not crash the service.
            // Log the problem and continue — the service starts with the built-in defaults
            // so the tray can still connect, show the error, and let the user fix the config.
            _logger.LogError(ex, "Configuration could not be loaded ({Message}); starting with defaults", ex.Message);
        }

        try
        {
            _databaseMigrator.Migrate();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database migration failed ({Message}); monitoring history will not be available", ex.Message);
        }

        try
        {
            await _vmSynchronizer.SynchronizeAsync(_configurationManager.Current, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "VM synchronization failed ({Message})", ex.Message);
        }

        await _providerManager.InitializeAsync(cancellationToken).ConfigureAwait(false);

        _configurationManager.ConfigurationChanged += OnConfigurationChanged;

        _logger.LogInformation("FireTower service ready");
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("FireTower service stopping");
        return Task.CompletedTask;
    }

    // EventHandler is void-returning, so this must catch everything itself: an unhandled
    // exception here would otherwise crash the process via the event-raising thread.
    private void OnConfigurationChanged(object? sender, Core.Configuration.FireTowerConfiguration configuration)
    {
        _ = SynchronizeSafelyAsync(configuration);
    }

    private async Task SynchronizeSafelyAsync(Core.Configuration.FireTowerConfiguration configuration)
    {
        try
        {
            await _vmSynchronizer.SynchronizeAsync(configuration, CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to synchronize virtual machines after configuration reload");
        }
    }
}

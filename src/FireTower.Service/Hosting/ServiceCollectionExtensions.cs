using FireTower.Core.Health;
using FireTower.Core.Health.Checks;
using FireTower.Core.Interfaces;
using FireTower.Core.Providers;
using FireTower.Core.Restart;
using FireTower.Core.Scheduling;
using FireTower.Core.Services;
using FireTower.Core.Utilities;
using FireTower.Data.Migrations;
using FireTower.Data.Repositories;
using FireTower.Data.Services;
using FireTower.Providers.VirtualBox;
using FireTower.Providers.VirtualBox.Commands;
using FireTower.Providers.VirtualBox.Services;
using FireTower.Service.Events;
using FireTower.Service.Ipc;
using FireTower.Service.Workers;
using Microsoft.Extensions.DependencyInjection;
using IConfigurationManager = FireTower.Core.Interfaces.IConfigurationManager;

namespace FireTower.Service.Hosting;

/// <summary>
/// Registers every FireTower service in one place, so Program.cs stays a thin composition
/// root rather than accumulating registrations as the application grows.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddFireTowerService(this IServiceCollection services)
    {
        services.AddSingleton<IConfigurationManager, Core.Configuration.ConfigurationManager>();
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<MonitoringState>();

        services.AddSingleton<ISqliteConnectionFactory, SqliteConnectionFactory>();
        services.AddSingleton<DatabaseMigrator>();
        services.AddSingleton<IVirtualMachineRepository, VirtualMachineRepository>();
        services.AddSingleton<IHealthHistoryRepository, HealthHistoryRepository>();
        services.AddSingleton<IRestartHistoryRepository, RestartHistoryRepository>();
        services.AddSingleton<IEventRepository, EventRepository>();
        services.AddSingleton<IStatisticsRepository, StatisticsRepository>();

        services.AddSingleton<EventPublisher>();
        services.AddSingleton<IEventPublisher>(sp => sp.GetRequiredService<EventPublisher>());

        services.AddSingleton<VirtualMachineSynchronizer>();
        services.AddSingleton<IProviderManager, ProviderManager>();
        services.AddSingleton<IVBoxManageLocator, VBoxManageLocator>();
        services.AddSingleton<IVBoxCommandRunner, VBoxCommandRunner>();
        services.AddSingleton<IVmProvider, VirtualBoxProvider>();

        services.AddSingleton<IHealthCheck, ProviderStatusHealthCheck>();
        services.AddSingleton<IHealthCheck, PingHealthCheck>();
        services.AddSingleton<IHealthCheck, TcpPortHealthCheck>();
        services.AddSingleton<IHealthCheck, GuestCommandHealthCheck>();
        services.AddSingleton<IHealthCheck, ProcessExistsHealthCheck>();
        services.AddSingleton<IHealthCheck, CustomCommandHealthCheck>();
        services.AddSingleton<HealthCheckStateTracker>();
        services.AddSingleton<HealthCheckExecutor>();
        services.AddSingleton<IHealthEngine, HealthEngine>();

        services.AddSingleton<RestartLockRegistry>();
        services.AddSingleton<IRestartEngine, RestartEngine>();

        services.AddSingleton<IScheduler, IntervalScheduler>();
        services.AddSingleton<IScheduledTask, HealthMonitorTask>();
        services.AddSingleton<IScheduledTask, StatisticsTask>();
        services.AddSingleton<IScheduledTask, RetentionCleanupTask>();

        services.AddScoped<FireTowerServiceHandler>();

        services.AddHostedService<StartupHostedService>();
        services.AddHostedService<SchedulerHostedService>();
        services.AddHostedService<NamedPipeIpcServer>();

        return services;
    }
}

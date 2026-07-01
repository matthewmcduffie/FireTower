using FireTower.Core.Interfaces;
using FireTower.Core.Scheduling;
using IConfigurationManager = FireTower.Core.Interfaces.IConfigurationManager;

namespace FireTower.Service.Workers;

/// <summary>
/// The Log Cleanup Worker described in service.md: purges database history older than the
/// configured retention windows so historical tables never grow without bound, per
/// database.md's Data Retention requirements.
/// </summary>
public sealed class RetentionCleanupTask : IScheduledTask
{
    public string Name => "Retention Cleanup";
    public TimeSpan Interval { get; }

    private readonly IHealthHistoryRepository _healthHistory;
    private readonly IRestartHistoryRepository _restartHistory;
    private readonly IEventRepository _eventRepository;
    private readonly IStatisticsRepository _statisticsRepository;
    private readonly IConfigurationManager _configurationManager;

    public RetentionCleanupTask(
        IHealthHistoryRepository healthHistory,
        IRestartHistoryRepository restartHistory,
        IEventRepository eventRepository,
        IStatisticsRepository statisticsRepository,
        IConfigurationManager configurationManager)
    {
        _healthHistory = healthHistory;
        _restartHistory = restartHistory;
        _eventRepository = eventRepository;
        _statisticsRepository = statisticsRepository;
        _configurationManager = configurationManager;
        Interval = TimeSpan.FromSeconds(Math.Max(300, configurationManager.Current.Global.RetentionCleanupIntervalSeconds));
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var global = _configurationManager.Current.Global;
        var now = DateTimeOffset.UtcNow;

        await _healthHistory.PurgeOlderThanAsync(now - TimeSpan.FromDays(global.HealthHistoryRetentionDays), cancellationToken).ConfigureAwait(false);
        await _restartHistory.PurgeOlderThanAsync(now - TimeSpan.FromDays(global.RestartHistoryRetentionDays), cancellationToken).ConfigureAwait(false);
        await _eventRepository.PurgeOlderThanAsync(now - TimeSpan.FromDays(global.EventRetentionDays), cancellationToken).ConfigureAwait(false);
        await _statisticsRepository.PurgeOlderThanAsync(now - TimeSpan.FromDays(global.StatisticsRetentionDays), cancellationToken).ConfigureAwait(false);
    }
}

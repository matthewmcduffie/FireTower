using FireTower.Core.Interfaces;
using FireTower.Core.Models;
using FireTower.Core.Scheduling;
using FireTower.Shared.Enums;
using IConfigurationManager = FireTower.Core.Interfaces.IConfigurationManager;

namespace FireTower.Service.Workers;

/// <summary>
/// Periodically records a fleet-wide statistics snapshot, per the Statistics Worker
/// suggestion and StatisticsSnapshots table in service.md and database.md.
/// </summary>
public sealed class StatisticsTask : IScheduledTask
{
    private const int SampleSizePerVm = 20;

    public string Name => "Statistics Snapshot";
    public TimeSpan Interval { get; }

    private readonly IVirtualMachineRepository _vmRepository;
    private readonly IRestartHistoryRepository _restartHistory;
    private readonly IHealthHistoryRepository _healthHistory;
    private readonly IStatisticsRepository _statisticsRepository;

    public StatisticsTask(
        IVirtualMachineRepository vmRepository,
        IRestartHistoryRepository restartHistory,
        IHealthHistoryRepository healthHistory,
        IStatisticsRepository statisticsRepository,
        IConfigurationManager configurationManager)
    {
        _vmRepository = vmRepository;
        _restartHistory = restartHistory;
        _healthHistory = healthHistory;
        _statisticsRepository = statisticsRepository;
        Interval = TimeSpan.FromSeconds(Math.Max(60, configurationManager.Current.Global.StatisticsSnapshotIntervalSeconds));
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var vms = await _vmRepository.GetAllAsync(cancellationToken).ConfigureAwait(false);

        var restartDurations = new List<double>();
        var healthCheckDurations = new List<double>();

        foreach (var vm in vms)
        {
            var recentRestarts = await _restartHistory.GetRecentAsync(vm.Id, SampleSizePerVm, cancellationToken).ConfigureAwait(false);
            restartDurations.AddRange(recentRestarts.Select(r => r.Duration.TotalSeconds));

            var recentHealth = await _healthHistory.GetRecentAsync(vm.Id, SampleSizePerVm, cancellationToken).ConfigureAwait(false);
            healthCheckDurations.AddRange(recentHealth.SelectMany(e => e.CheckResults).Select(c => c.Duration.TotalMilliseconds));
        }

        var snapshot = new StatisticsSnapshot
        {
            Timestamp = DateTimeOffset.UtcNow,
            TotalVmCount = vms.Count,
            HealthyCount = vms.Count(v => v.Health == HealthState.Healthy),
            WarningCount = vms.Count(v => v.Health == HealthState.Warning),
            CriticalCount = vms.Count(v => v.Health is HealthState.Critical or HealthState.Offline or HealthState.Degraded),
            RestartCount = vms.Sum(v => v.RestartCount),
            AverageRestartDurationSeconds = restartDurations.Count > 0 ? restartDurations.Average() : 0,
            AverageHealthCheckDurationMs = healthCheckDurations.Count > 0 ? healthCheckDurations.Average() : 0,
        };

        await _statisticsRepository.RecordAsync(snapshot, cancellationToken).ConfigureAwait(false);
    }
}

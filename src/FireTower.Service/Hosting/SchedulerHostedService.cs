using FireTower.Core.Scheduling;
using Microsoft.Extensions.Hosting;

namespace FireTower.Service.Hosting;

/// <summary>
/// Starts and stops the <see cref="IScheduler"/> alongside the host's own lifetime, after
/// every <see cref="IScheduledTask"/> has been registered with it.
/// </summary>
public sealed class SchedulerHostedService : IHostedService
{
    private readonly IScheduler _scheduler;

    public SchedulerHostedService(IScheduler scheduler, IEnumerable<IScheduledTask> tasks)
    {
        _scheduler = scheduler;
        foreach (var task in tasks)
        {
            _scheduler.Register(task);
        }
    }

    public Task StartAsync(CancellationToken cancellationToken) => _scheduler.StartAsync(cancellationToken);

    public Task StopAsync(CancellationToken cancellationToken) => _scheduler.StopAsync(cancellationToken);
}

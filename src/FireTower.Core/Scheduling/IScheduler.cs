namespace FireTower.Core.Scheduling;

/// <summary>
/// Runs registered <see cref="IScheduledTask"/> instances at their configured interval.
/// Workers register tasks instead of implementing their own fixed-delay loops, so new
/// scheduled work can be added without touching the scheduling mechanism itself.
/// </summary>
public interface IScheduler
{
    void Register(IScheduledTask task);

    Task StartAsync(CancellationToken cancellationToken);

    Task StopAsync(CancellationToken cancellationToken);
}

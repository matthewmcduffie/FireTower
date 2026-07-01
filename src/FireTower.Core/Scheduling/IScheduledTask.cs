namespace FireTower.Core.Scheduling;

/// <summary>
/// A unit of recurring work registered with <see cref="IScheduler"/>, such as a health
/// check pass, log cleanup, or a statistics snapshot.
/// </summary>
public interface IScheduledTask
{
    string Name { get; }

    TimeSpan Interval { get; }

    Task RunAsync(CancellationToken cancellationToken);
}

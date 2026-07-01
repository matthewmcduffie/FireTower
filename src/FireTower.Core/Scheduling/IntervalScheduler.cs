using Microsoft.Extensions.Logging;

namespace FireTower.Core.Scheduling;

/// <summary>
/// Default <see cref="IScheduler"/> implementation. Each registered task runs on its own
/// independent loop at its configured interval, so a slow or failing task never delays
/// any other task.
/// </summary>
public sealed class IntervalScheduler : IScheduler
{
    private readonly ILogger<IntervalScheduler> _logger;
    private readonly List<IScheduledTask> _tasks = new();
    private readonly List<Task> _runningLoops = new();
    private CancellationTokenSource? _stoppingTokenSource;

    public IntervalScheduler(ILogger<IntervalScheduler> logger)
    {
        _logger = logger;
    }

    public void Register(IScheduledTask task)
    {
        _tasks.Add(task);
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _stoppingTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        foreach (var task in _tasks)
        {
            _runningLoops.Add(RunLoopAsync(task, _stoppingTokenSource.Token));
        }

        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _stoppingTokenSource?.Cancel();
        await Task.WhenAll(_runningLoops).ConfigureAwait(false);
    }

    private async Task RunLoopAsync(IScheduledTask task, CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(task.Interval);
        while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
        {
            try
            {
                await task.RunAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Scheduled task {TaskName} failed", task.Name);
            }
        }
    }
}

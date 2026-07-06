using System.Collections.Concurrent;
using System.Runtime.Versioning;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ShippingGuard.Agent.Ipc;
using ShippingGuard.Agent.Monitoring;
using ShippingGuard.Core.Configuration;
using ShippingGuard.Core.Models;

namespace ShippingGuard.Agent.Workers;

[SupportedOSPlatform("windows")]
public sealed class MonitorWorker : BackgroundService
{
    private readonly AgentSettings _settings;
    private readonly ProcessMonitor _monitor;
    private readonly ILogger<MonitorWorker> _logger;

    private readonly ConcurrentDictionary<string, AppStatus> _statuses = new();
    private readonly ConcurrentDictionary<string, bool> _maintenanceMode = new();
    private AgentIpcServer? _ipc;

    public MonitorWorker(AgentSettings settings, ProcessMonitor monitor, ILogger<MonitorWorker> logger)
    {
        _settings = settings;
        _monitor = monitor;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stopping)
    {
        using var ipc = new AgentIpcServer(_settings.PipeName, HandleIpcRequest,
            _logger as ILogger<AgentIpcServer> ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<AgentIpcServer>.Instance);
        _ipc = ipc;
        ipc.Start(stopping);

        _logger.LogInformation("ShippingGuard monitor started. Profiles: {Dir}", _settings.ProfilesDirectory);

        while (!stopping.IsCancellationRequested)
        {
            var profiles = ProfileLoader.LoadAll(_settings.ProfilesDirectory);

            foreach (var profile in profiles.Where(p => p.Enabled))
            {
                await EvaluateAsync(profile, stopping);
            }

            // Clean up statuses for removed profiles
            var activeIds = profiles.Select(p => p.Id).ToHashSet();
            foreach (var key in _statuses.Keys.Except(activeIds).ToList())
                _statuses.TryRemove(key, out _);

            var interval = profiles.Count > 0
                ? TimeSpan.FromSeconds(Math.Max(5, profiles.Min(p => p.CheckIntervalSeconds)))
                : TimeSpan.FromSeconds(10);
            try { await Task.Delay(interval, stopping); }
            catch (OperationCanceledException) { break; }
        }
    }

    private async Task EvaluateAsync(AppProfile profile, CancellationToken ct)
    {
        var status = _statuses.GetOrAdd(profile.Id, id => new AppStatus
        {
            ProfileId = id,
            DisplayName = profile.DisplayName,
        });

        status.Enabled = profile.Enabled;
        status.MaintenanceMode = _maintenanceMode.GetValueOrDefault(profile.Id);

        // Check if hung first
        if (profile.KillIfHung && _monitor.IsHung(profile, out var hungPid))
        {
            status.IsHung = true;
            status.Health = AppHealthState.Hung;
            status.LastError = $"Process {profile.ProcessName} (PID {hungPid}) is not responding.";
            _logger.LogWarning("[{App}] Process is hung (PID {Pid}). Killing.", profile.DisplayName, hungPid);
            _monitor.TryKill(profile, force: true);
            status.LastCrashed = DateTimeOffset.UtcNow;
            await Task.Delay(2000, ct);
        }

        status.IsRunning = _monitor.IsRunning(profile, out var pid);
        status.ProcessId = pid;
        status.IsHung = false;

        if (status.IsRunning)
        {
            status.Health = status.MaintenanceMode ? AppHealthState.MaintenanceMode : AppHealthState.Running;
            return;
        }

        // Not running
        status.Health = AppHealthState.Stopped;

        if (!profile.RestartIfMissing) return;

        if (_monitor.IsRetryLimitReached(profile))
        {
            status.Health = AppHealthState.RetryLimitReached;
            status.LastError = $"Retry limit ({profile.MaxRestartAttempts}) reached. Manual intervention required.";
            return;
        }

        _logger.LogInformation("[{App}] Not running. Attempting start.", profile.DisplayName);
        status.Health = AppHealthState.Restarting;
        status.LastAction = $"Starting at {DateTimeOffset.Now:T}";

        await Task.Delay(TimeSpan.FromSeconds(profile.StartupDelaySeconds), ct);

        if (_monitor.TryStart(profile, out var newPid))
        {
            status.IsRunning = true;
            status.ProcessId = newPid;
            status.LastStarted = DateTimeOffset.UtcNow;
            status.Health = AppHealthState.Running;
            status.LastError = null;
            status.RestartCount++;
            _logger.LogInformation("[{App}] Started (PID {Pid}). Total restarts: {N}", profile.DisplayName, newPid, status.RestartCount);
        }
        else
        {
            status.Health = AppHealthState.Stopped;
            status.LastError = "Failed to start. Check executable path in profile.";
        }
    }

    private IpcResponse HandleIpcRequest(IpcRequest req)
    {
        try
        {
            switch (req.Command)
            {
                case IpcCommands.GetStatus:
                    return IpcResponse.Ok(_statuses.Values.ToList());

                case IpcCommands.SetMaintenance when req.ProfileId is not null:
                    _maintenanceMode[req.ProfileId] = req.Value ?? false;
                    return IpcResponse.Ok();

                case IpcCommands.SetEnabled when req.ProfileId is not null:
                    if (_statuses.TryGetValue(req.ProfileId, out var s))
                        s.Enabled = req.Value ?? true;
                    return IpcResponse.Ok();

                case IpcCommands.KillApp when req.ProfileId is not null:
                {
                    var profiles = ProfileLoader.LoadAll(_settings.ProfilesDirectory);
                    var p = profiles.FirstOrDefault(x => x.Id == req.ProfileId);
                    if (p is null) return IpcResponse.Fail("Profile not found.");
                    _monitor.TryKill(p, force: true);
                    return IpcResponse.Ok();
                }

                case IpcCommands.StartApp when req.ProfileId is not null:
                {
                    var profiles = ProfileLoader.LoadAll(_settings.ProfilesDirectory);
                    var p = profiles.FirstOrDefault(x => x.Id == req.ProfileId);
                    if (p is null) return IpcResponse.Fail("Profile not found.");
                    _monitor.TryStart(p, out _);
                    return IpcResponse.Ok();
                }

                case IpcCommands.StopApp when req.ProfileId is not null:
                {
                    var profiles = ProfileLoader.LoadAll(_settings.ProfilesDirectory);
                    var p = profiles.FirstOrDefault(x => x.Id == req.ProfileId);
                    if (p is null) return IpcResponse.Fail("Profile not found.");
                    _monitor.TryKill(p, force: false);
                    return IpcResponse.Ok();
                }

                default:
                    return IpcResponse.Fail($"Unknown command: {req.Command}");
            }
        }
        catch (Exception ex)
        {
            return IpcResponse.Fail(ex.Message);
        }
    }
}

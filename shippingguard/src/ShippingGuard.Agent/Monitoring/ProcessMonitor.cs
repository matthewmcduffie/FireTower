using System.Diagnostics;
using System.Runtime.Versioning;
using Microsoft.Extensions.Logging;
using ShippingGuard.Agent.Platform;
using ShippingGuard.Core.Models;

namespace ShippingGuard.Agent.Monitoring;

[SupportedOSPlatform("windows")]
public sealed class ProcessMonitor
{
    private readonly RestartTracker _tracker;
    private readonly ILogger<ProcessMonitor> _logger;

    public ProcessMonitor(RestartTracker tracker, ILogger<ProcessMonitor> logger)
    {
        _tracker = tracker;
        _logger = logger;
    }

    public bool IsRunning(AppProfile profile, out int pid)
    {
        var procs = Process.GetProcessesByName(profile.ProcessName);
        pid = procs.FirstOrDefault()?.Id ?? 0;
        return procs.Length > 0;
    }

    public bool IsHung(AppProfile profile, out int pid)
    {
        pid = 0;
        var procs = Process.GetProcessesByName(profile.ProcessName);
        if (procs.Length == 0) return false;
        var p = procs.First();
        pid = p.Id;
        if (p.MainWindowHandle == IntPtr.Zero) return false;
        return !p.Responding;
    }

    public bool TryStart(AppProfile profile, out int pid)
    {
        pid = 0;
        var (exe, args) = profile.ResolveCommand();
        if (string.IsNullOrWhiteSpace(exe))
        {
            _logger.LogWarning("[{App}] No executable configured.", profile.DisplayName);
            return false;
        }

        _logger.LogInformation("[{App}] Starting: {Exe} {Args}", profile.DisplayName, exe, args);

        if (SessionHelper.TryLaunchInUserSession(exe, args, out pid))
        {
            _logger.LogInformation("[{App}] Started in user session (PID {Pid}).", profile.DisplayName, pid);
            _tracker.Record(profile.Id);
            return true;
        }

        // Fallback: launch directly (useful when running interactively)
        try
        {
            var p = Process.Start(new ProcessStartInfo(exe, args) { UseShellExecute = true });
            if (p is not null)
            {
                pid = p.Id;
                _tracker.Record(profile.Id);
                _logger.LogInformation("[{App}] Started directly (PID {Pid}).", profile.DisplayName, pid);
                return true;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{App}] Failed to start.", profile.DisplayName);
        }

        return false;
    }

    public bool TryKill(AppProfile profile, bool force)
    {
        var procs = Process.GetProcessesByName(profile.ProcessName);
        if (procs.Length == 0) return true;

        foreach (var p in procs)
        {
            try
            {
                if (force)
                {
                    p.Kill(entireProcessTree: true);
                    _logger.LogInformation("[{App}] Force-killed PID {Pid}.", profile.DisplayName, p.Id);
                }
                else
                {
                    p.CloseMainWindow();
                    if (!p.WaitForExit(5000)) p.Kill(entireProcessTree: true);
                    _logger.LogInformation("[{App}] Gracefully closed PID {Pid}.", profile.DisplayName, p.Id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[{App}] Could not kill PID {Pid}.", profile.DisplayName, p.Id);
            }
        }
        return true;
    }

    public bool IsRetryLimitReached(AppProfile profile) =>
        _tracker.GetCount(profile.Id, profile.RestartCooldownSeconds * profile.MaxRestartAttempts)
        >= profile.MaxRestartAttempts;
}

using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Core;
using ShippingGuard.Agent.Monitoring;
using ShippingGuard.Agent.Workers;
using ShippingGuard.Core.Configuration;

var settings = new AgentSettings();
settings.EnsureDirectoriesExist();

// Self-registration
if (args.Contains("--install"))
{
    Run("sc", "stop ShippingGuard");
    Thread.Sleep(2000);
    Run("sc", "delete ShippingGuard");
    Thread.Sleep(2000);
    var path = Environment.ProcessPath!;
    for (int i = 0; i < 5; i++)
    {
        int code = PS($"New-Service -Name ShippingGuard -BinaryPathName '{path}' " +
                      "-StartupType Automatic " +
                      "-DisplayName 'ShippingGuard App Watchdog' " +
                      "-Description 'Monitors shipping applications and restarts them if they stop.'");
        if (code == 0) break;
        Thread.Sleep(2000);
    }
    Run("sc", "failure ShippingGuard reset= 86400 actions= restart/5000/restart/10000/restart/30000");
    PS("Start-Service -Name ShippingGuard -ErrorAction SilentlyContinue");
    return;
}

if (args.Contains("--uninstall"))
{
    Run("sc", "stop ShippingGuard");
    Thread.Sleep(2000);
    Run("sc", "delete ShippingGuard");
    return;
}

// Normal service run
var levelSwitch = new LoggingLevelSwitch();
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.ControlledBy(levelSwitch)
    .Enrich.FromLogContext()
    .WriteTo.File(Path.Combine(settings.LogsDirectory, "agent.log"),
        rollingInterval: RollingInterval.Day, retainedFileCountLimit: 30)
    .WriteTo.Console()
    .CreateLogger();

try
{
    var host = Host.CreateApplicationBuilder(args);
    host.Services.AddWindowsService(o => o.ServiceName = "ShippingGuard");
    host.Services.AddSerilog();
    host.Services.AddSingleton(settings);
    host.Services.AddSingleton<RestartTracker>();
    host.Services.AddSingleton<ProcessMonitor>();
    host.Services.AddHostedService<MonitorWorker>();

    var app = host.Build();
    app.Run();
}
finally
{
    Log.CloseAndFlush();
}

static void Run(string exe, string arguments)
{
    using var p = Process.Start(new ProcessStartInfo(exe, arguments)
    { UseShellExecute = false, CreateNoWindow = true });
    p?.WaitForExit();
}

static int PS(string command)
{
    using var p = Process.Start(new ProcessStartInfo(
        "powershell.exe", $"-NoProfile -NonInteractive -Command \"{command}\"")
    { UseShellExecute = false, CreateNoWindow = true });
    p?.WaitForExit();
    return p?.ExitCode ?? -1;
}

using System.Diagnostics;
using FireTower.Core.Configuration.Paths;
using FireTower.Service.Hosting;
using FireTower.Service.Logging;
using Serilog;
using Serilog.Core;

// Self-registration so the MSI installer can call us directly rather than
// relying on WiX service table entries, which are unreliable in WiX v5.0.2.
if (args.Contains("--install"))
{
    // Remove any existing registration (handles upgrades and "marked for deletion"
    // scenarios where a previous install left a stale entry).
    Run("sc", "stop FireTower");
    Thread.Sleep(3000);
    Run("sc", "delete FireTower");
    Thread.Sleep(2000);

    var exe = $"\"{Environment.ProcessPath}\"";

    // Retry creation in case SCM hasn't finished processing the delete yet.
    for (int attempt = 0; attempt < 5; attempt++)
    {
        int code = RunExitCode("sc", $"create FireTower binPath= {exe} start= auto obj= LocalSystem DisplayName= \"FireTower VM Watchdog\"");
        if (code == 0) break;
        Thread.Sleep(2000);
    }

    Run("sc", "description FireTower \"Monitors and automatically recovers VirtualBox virtual machines.\"");
    Run("sc", "failure FireTower reset= 86400 actions= restart/5000/restart/10000/restart/30000");
    Run("sc", "start FireTower");
    return;
}

if (args.Contains("--uninstall"))
{
    Run("sc", "stop FireTower");
    Thread.Sleep(3000);
    Run("sc", "delete FireTower");
    return;
}

var paths = new FireTowerPaths();
paths.EnsureDirectoriesExist();
var levelSwitch = new LoggingLevelSwitch();
var logStore = new InMemoryLogStore();
Log.Logger = SerilogConfigurator.Build(paths, levelSwitch, logStore);

try
{
    var builder = Host.CreateApplicationBuilder(args);
    builder.Services.AddWindowsService(options => options.ServiceName = "FireTower");
    builder.Services.AddSerilog();
    builder.Services.AddSingleton<IFireTowerPaths>(paths);
    builder.Services.AddSingleton(levelSwitch);
    builder.Services.AddSingleton(logStore);
    builder.Services.AddFireTowerService();

    var host = builder.Build();
    host.Run();
}
finally
{
    Log.CloseAndFlush();
}

static void Run(string exe, string args) => RunExitCode(exe, args);

static int RunExitCode(string exe, string args)
{
    using var p = Process.Start(new ProcessStartInfo(exe, args)
    {
        UseShellExecute = false,
        CreateNoWindow  = true,
    });
    p?.WaitForExit();
    return p?.ExitCode ?? -1;
}

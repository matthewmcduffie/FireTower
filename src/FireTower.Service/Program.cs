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
    var exe = $"\"{Environment.ProcessPath}\"";
    Run("sc", $"create FireTower binPath= {exe} start= auto obj= LocalSystem DisplayName= \"FireTower VM Watchdog\"");
    Run("sc", "description FireTower \"Monitors and automatically recovers VirtualBox virtual machines.\"");
    Run("sc", "failure FireTower reset= 86400 actions= restart/5000/restart/10000/restart/30000");
    return;
}

if (args.Contains("--uninstall"))
{
    Run("sc", "stop FireTower");
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

static void Run(string exe, string args)
{
    using var p = Process.Start(new ProcessStartInfo(exe, args)
    {
        UseShellExecute = false,
        CreateNoWindow  = true,
    });
    p?.WaitForExit();
}

using System.Diagnostics;
using FireTower.Core.Configuration.Paths;
using FireTower.Service.Hosting;
using FireTower.Service.Logging;
using Serilog;
using Serilog.Core;

if (args.Contains("--install"))
{
    // Clean up any existing registration.
    // sc.exe stop/delete used for removal (available on all Windows versions).
    Run("sc", "stop FireTower");
    Thread.Sleep(3000);
    Run("sc", "delete FireTower");
    Thread.Sleep(2000);

    // Use PowerShell New-Service rather than sc.exe for creation because
    // sc.exe strips the surrounding quotes from binPath and writes the bare
    // path to the registry — which breaks service startup when the install
    // directory contains spaces (C:\Program Files\...).
    // New-Service writes the correct quoted ImagePath automatically.
    var path = Environment.ProcessPath!;
    var description = "Monitors and automatically recovers VirtualBox virtual machines.";

    for (int attempt = 0; attempt < 5; attempt++)
    {
        int code = PS($"New-Service -Name FireTower -BinaryPathName '{path}' " +
                      $"-StartupType Automatic " +
                      $"-DisplayName 'FireTower VM Watchdog' " +
                      $"-Description '{description}'");
        if (code == 0) break;
        Thread.Sleep(2000);
    }

    Run("sc", "failure FireTower reset= 86400 actions= restart/5000/restart/10000/restart/30000");
    PS("Start-Service -Name FireTower -ErrorAction SilentlyContinue");
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

static void Run(string exe, string arguments)
{
    using var p = Process.Start(new ProcessStartInfo(exe, arguments)
    {
        UseShellExecute = false,
        CreateNoWindow  = true,
    });
    p?.WaitForExit();
}

static int PS(string command)
{
    using var p = Process.Start(new ProcessStartInfo(
        "powershell.exe",
        $"-NoProfile -NonInteractive -Command \"{command}\"")
    {
        UseShellExecute = false,
        CreateNoWindow  = true,
    });
    p?.WaitForExit();
    return p?.ExitCode ?? -1;
}

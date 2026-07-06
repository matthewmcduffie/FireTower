using System.Diagnostics;
using FireTower.Core.Configuration.Paths;
using FireTower.Service.Hosting;
using FireTower.Service.Logging;
using Serilog;
using Serilog.Core;

if (args.Contains("--install"))
{
    // Stop and remove any existing registration.
    Run("sc", "stop FireTower");
    Thread.Sleep(2000);
    Run("sc", "delete FireTower");

    // Wait until the SCM has fully released the old registration before trying
    // to create a new one. A fixed sleep is not reliable — on a slow machine or
    // after an upgrade the service can remain "marked for deletion" for several
    // seconds after sc delete returns. Poll instead.
    for (int i = 0; i < 20; i++)
    {
        if (RunExitCode("sc", "query FireTower") != 0) break;
        Thread.Sleep(1000);
    }

    // Use PowerShell New-Service rather than sc.exe because sc.exe strips
    // surrounding quotes from binPath — breaking startup when the path contains
    // spaces (e.g. C:\Program Files\...).
    var path = Environment.ProcessPath!;
    var description = "Monitors and automatically recovers VirtualBox virtual machines.";

    // Wrap the path in double-quotes so the ImagePath registry value is stored as
    // "C:\Program Files\FireTower\FireTower.Service.exe" — required when the path
    // contains spaces, otherwise SCM parses the command line at the first space and
    // tries to launch "C:\Program" which does not exist.
    for (int attempt = 0; attempt < 5; attempt++)
    {
        int code = PS($"New-Service -Name FireTower -BinaryPathName '\"{path}\"' " +
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

static void Run(string exe, string arguments) => RunExitCode(exe, arguments);

static int RunExitCode(string exe, string arguments)
{
    using var p = Process.Start(new ProcessStartInfo(exe, arguments)
    {
        UseShellExecute = false,
        CreateNoWindow  = true,
    });
    p?.WaitForExit();
    return p?.ExitCode ?? -1;
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

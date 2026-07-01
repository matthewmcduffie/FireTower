using FireTower.Core.Configuration.Paths;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace FireTower.Service.Logging;

/// <summary>
/// Builds the Serilog pipeline described in logging.md: one rolling file per concern
/// (service, provider, health, restart, ipc) plus a catch-all application log, with
/// critical events also forwarded to the Windows Event Log.
/// </summary>
public static class SerilogConfigurator
{
    public static Serilog.ILogger Build(IFireTowerPaths paths, LoggingLevelSwitch levelSwitch, InMemoryLogStore logStore)
    {
        Directory.CreateDirectory(paths.LogsDirectory);

        var configuration = new LoggerConfiguration()
            .MinimumLevel.ControlledBy(levelSwitch)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("MachineName", Environment.MachineName)
            .WriteTo.Sink(new InMemoryLogSink(logStore))
            .WriteTo.Logger(sub => ConfigureComponentFile(sub, paths, "service.log", "FireTower.Service"))
            .WriteTo.Logger(sub => ConfigureComponentFile(sub, paths, "provider.log", "FireTower.Providers"))
            .WriteTo.Logger(sub => ConfigureComponentFile(sub, paths, "health.log", "FireTower.Core.Health"))
            .WriteTo.Logger(sub => ConfigureComponentFile(sub, paths, "restart.log", "FireTower.Core.Restart"))
            .WriteTo.Logger(sub => ConfigureComponentFile(sub, paths, "ipc.log", "FireTower.Service.Ipc"))
            .WriteTo.File(
                Path.Combine(paths.LogsDirectory, "application.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30,
                outputTemplate: OutputTemplate);

        if (Environment.UserInteractive)
        {
            configuration.WriteTo.Console(outputTemplate: OutputTemplate);
        }

        TryAddEventLogSink(configuration);

        return configuration.CreateLogger();
    }

    private static void ConfigureComponentFile(LoggerConfiguration sub, IFireTowerPaths paths, string fileName, string sourceContextPrefix)
    {
        sub.Filter.ByIncludingOnly(e =>
                e.Properties.TryGetValue("SourceContext", out var value) &&
                value.ToString().Trim('"').StartsWith(sourceContextPrefix, StringComparison.Ordinal))
            .WriteTo.File(
                Path.Combine(paths.LogsDirectory, fileName),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30,
                outputTemplate: OutputTemplate);
    }

    private static void TryAddEventLogSink(LoggerConfiguration configuration)
    {
        try
        {
            configuration.WriteTo.EventLog(
                "FireTower",
                manageEventSource: true,
                restrictedToMinimumLevel: LogEventLevel.Warning);
        }
        catch (Exception)
        {
            // Creating the event source requires administrative privileges on first run.
            // Falling back to file-only logging keeps the service usable without elevation.
        }
    }

    private const string OutputTemplate =
        "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {SourceContext}{NewLine}{Message:lj}{NewLine}{Exception}";
}

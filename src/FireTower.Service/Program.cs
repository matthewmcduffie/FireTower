using FireTower.Core.Configuration.Paths;
using FireTower.Service.Hosting;
using FireTower.Service.Logging;
using Serilog;
using Serilog.Core;

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

using FireTower.Core.Configuration.Paths;
using FireTower.Service.Hosting;
using FireTower.Service.Logging;
using FireTower.Shared.Enums;
using FireTower.Tray.Services.Ipc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Serilog.Core;

namespace FireTower.Tests.Ipc;

/// <summary>
/// End-to-end verification that the Named Pipe IPC server and client can actually complete
/// a round trip: connect, issue a request, and receive a typed response. This is an
/// integration test (real host, real pipe), not a unit test, per testing.md's IPC Testing
/// section.
/// </summary>
public sealed class NamedPipeIpcSmokeTests : IAsyncLifetime
{
    private readonly string _dataDirectory = Path.Combine(Path.GetTempPath(), "firetower-ipc-test-" + Guid.NewGuid());
    private IHost? _host;

    public async Task InitializeAsync()
    {
        Environment.SetEnvironmentVariable("FIRETOWER_DATA_DIR", _dataDirectory);

        var paths = new FireTowerPaths();
        paths.EnsureDirectoriesExist();

        var builder = Host.CreateApplicationBuilder();
        builder.Services.AddSingleton<IFireTowerPaths>(paths);
        builder.Services.AddSingleton(new LoggingLevelSwitch());
        builder.Services.AddSingleton(new InMemoryLogStore());
        builder.Services.AddLogging(b => b.AddProvider(NullLoggerProvider.Instance));
        builder.Services.AddFireTowerService();

        _host = builder.Build();
        await _host.StartAsync();

        // Give the accept loop a moment to bind the pipe before the client tries to connect.
        await Task.Delay(500);
    }

    public async Task DisposeAsync()
    {
        if (_host is not null)
        {
            await _host.StopAsync();
            _host.Dispose();
        }

        Environment.SetEnvironmentVariable("FIRETOWER_DATA_DIR", null);

        if (Directory.Exists(_dataDirectory))
        {
            Directory.Delete(_dataDirectory, recursive: true);
        }
    }

    [Fact]
    public async Task Client_can_connect_and_retrieve_service_status()
    {
        var client = new FireTowerIpcClient(NullLogger<FireTowerIpcClient>.Instance);
        client.Start();

        try
        {
            var deadline = DateTime.UtcNow.AddSeconds(10);
            while (client.State != ConnectionState.Connected && DateTime.UtcNow < deadline)
            {
                await Task.Delay(100);
            }

            Assert.Equal(ConnectionState.Connected, client.State);

            var result = await client.Service.GetServiceStatusAsync(CancellationToken.None);

            Assert.True(result.Success);
            Assert.NotNull(result.Payload);
            Assert.Equal(ServiceHealthState.Running, result.Payload!.State);
        }
        finally
        {
            await client.StopAsync();
        }
    }
}

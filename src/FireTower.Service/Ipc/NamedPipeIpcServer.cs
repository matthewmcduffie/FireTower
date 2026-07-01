using System.Collections.Concurrent;
using System.IO.Pipes;
using FireTower.Core.Interfaces;
using FireTower.Core.Models;
using FireTower.Service.Events;
using FireTower.Shared.Constants;
using FireTower.Shared.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StreamJsonRpc;
using IConfigurationManager = FireTower.Core.Interfaces.IConfigurationManager;

namespace FireTower.Service.Ipc;

/// <summary>
/// Hosts the Named Pipe server described in ipc.md: accepts client connections, exposes
/// <see cref="IFireTowerService"/> over StreamJsonRpc, and forwards events from
/// <see cref="EventPublisher"/> to every connected client. Each connection runs
/// independently so one slow client never blocks another.
/// </summary>
public sealed class NamedPipeIpcServer : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfigurationManager _configurationManager;
    private readonly EventPublisher _eventPublisher;
    private readonly ILogger<NamedPipeIpcServer> _logger;

    private readonly ConcurrentDictionary<Guid, JsonRpc> _clients = new();
    private CancellationTokenSource? _stoppingTokenSource;
    private Task? _acceptLoop;
    private bool _loggedAclFallbackWarning;

    public NamedPipeIpcServer(
        IServiceProvider serviceProvider,
        IConfigurationManager configurationManager,
        EventPublisher eventPublisher,
        ILogger<NamedPipeIpcServer> logger)
    {
        _serviceProvider = serviceProvider;
        _configurationManager = configurationManager;
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _stoppingTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _eventPublisher.VmStatusChanged += OnVmStatusChanged;
        _eventPublisher.ConfigurationReloaded += OnConfigurationReloaded;
        _eventPublisher.ProviderStatusChanged += OnProviderStatusChanged;

        _acceptLoop = AcceptLoopAsync(_stoppingTokenSource.Token);
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _eventPublisher.VmStatusChanged -= OnVmStatusChanged;
        _eventPublisher.ConfigurationReloaded -= OnConfigurationReloaded;
        _eventPublisher.ProviderStatusChanged -= OnProviderStatusChanged;

        if (_stoppingTokenSource is not null)
        {
            await _stoppingTokenSource.CancelAsync().ConfigureAwait(false);
        }

        foreach (var client in _clients.Values)
        {
            // JsonRpc does not implement IAsyncDisposable; Dispose() here is synchronous
            // by necessity, not an oversight.
            client.Dispose();
        }

        if (_acceptLoop is not null)
        {
            // This loop is started fire-and-forget from StartAsync (the standard
            // IHostedService pattern), not from a tracked async context. VSTHRD003
            // targets Visual Studio extensions with a JoinableTaskFactory; a Windows
            // Service host has none, so the deadlock this rule guards against cannot occur.
#pragma warning disable VSTHRD003
            await _acceptLoop.ConfigureAwait(false);
#pragma warning restore VSTHRD003
        }
    }

    private async Task AcceptLoopAsync(CancellationToken stoppingToken)
    {
        var pipeName = _configurationManager.Current.Global.Ipc.PipeName;
        if (string.IsNullOrWhiteSpace(pipeName))
        {
            pipeName = IpcDefaults.PipeName;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var stream = CreateServerStream(pipeName);
                await stream.WaitForConnectionAsync(stoppingToken).ConfigureAwait(false);
                _ = HandleClientAsync(stream, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to accept an IPC client connection");

                // Without a delay, a persistent failure (e.g. the pipe name being
                // unavailable) would spin this loop as fast as the CPU allows.
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }
    }

    private NamedPipeServerStream CreateServerStream(string pipeName)
    {
        try
        {
            return NamedPipeServerStreamAcl.Create(
                pipeName,
                PipeDirection.InOut,
                NamedPipeServerStream.MaxAllowedServerInstances,
                PipeTransmissionMode.Byte,
                PipeOptions.Asynchronous,
                inBufferSize: 0,
                outBufferSize: 0,
                pipeSecurity: PipeSecurityFactory.Create());
        }
        catch (UnauthorizedAccessException)
        {
            // Setting an explicit ACL requires privileges the process may not hold when
            // not running elevated (e.g. local development or test runs). The production
            // Windows Service runs elevated and will use the hardened ACL; everywhere else
            // falls back to the default pipe security rather than failing to start.
            if (!_loggedAclFallbackWarning)
            {
                _logger.LogWarning(
                    "Could not apply the hardened Named Pipe ACL (the process is not elevated); falling back to default pipe security.");
                _loggedAclFallbackWarning = true;
            }

            return new NamedPipeServerStream(
                pipeName,
                PipeDirection.InOut,
                NamedPipeServerStream.MaxAllowedServerInstances,
                PipeTransmissionMode.Byte,
                PipeOptions.Asynchronous);
        }
    }

    private async Task HandleClientAsync(NamedPipeServerStream stream, CancellationToken stoppingToken)
    {
        var clientId = Guid.NewGuid();
        using var scope = _serviceProvider.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<FireTowerServiceHandler>();

        var jsonRpc = new JsonRpc(stream);
        jsonRpc.AddLocalRpcTarget<IFireTowerService>(handler, null);
        _clients[clientId] = jsonRpc;
        jsonRpc.StartListening();

        _logger.LogInformation("IPC client connected ({ClientId})", clientId);

        try
        {
            await jsonRpc.Completion.ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "IPC client {ClientId} connection ended", clientId);
        }
        finally
        {
            _clients.TryRemove(clientId, out _);
            // Disposing JsonRpc also disposes the underlying stream it was constructed with.
            jsonRpc.Dispose();
            _logger.LogInformation("IPC client disconnected ({ClientId})", clientId);
        }
    }

    private void OnVmStatusChanged(object? sender, VirtualMachine vm) =>
        BroadcastSafely(nameof(IFireTowerEventSink.OnVmStatusChangedAsync), DtoMapping.ToStatusChangedDto(vm));

    private void OnConfigurationReloaded(object? sender, EventArgs e) =>
        BroadcastSafely(nameof(IFireTowerEventSink.OnConfigurationReloadedAsync));

    private void OnProviderStatusChanged(object? sender, ProviderRegistration registration) =>
        BroadcastSafely(nameof(IFireTowerEventSink.OnProviderStatusChangedAsync), DtoMapping.ToDto(registration));

    private void BroadcastSafely(string methodName, params object[] arguments)
    {
        _ = BroadcastAsync(methodName, arguments);
    }

    private async Task BroadcastAsync(string methodName, object[] arguments)
    {
        foreach (var client in _clients.Values)
        {
            try
            {
                await client.NotifyAsync(methodName, arguments).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to deliver {Method} notification to a client", methodName);
            }
        }
    }
}

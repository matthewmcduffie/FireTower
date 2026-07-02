using System.IO.Pipes;
using FireTower.Shared.Constants;
using FireTower.Shared.Contracts;
using FireTower.Shared.DTOs;
using FireTower.Shared.Enums;
using Microsoft.Extensions.Logging;
using StreamJsonRpc;

namespace FireTower.Tray.Services.Ipc;

/// <summary>
/// Default <see cref="IFireTowerIpcClient"/> implementation. Maintains a Named Pipe
/// connection to the Windows Service, reconnecting automatically with backoff, and
/// receives server-pushed events through <see cref="IFireTowerEventSink"/>, per the
/// Connection Management requirements in ipc.md.
/// </summary>
public sealed class FireTowerIpcClient : IFireTowerIpcClient, IFireTowerEventSink, IDisposable
{
    // Retry every 2 seconds — no backing off to 10+ seconds.
    // After a fresh install the service may take 5-15 seconds to start and we
    // want the tray to pick it up quickly the moment the pipe becomes available.
    private static readonly TimeSpan ReconnectInterval = TimeSpan.FromSeconds(2);

    private readonly ILogger<FireTowerIpcClient> _logger;
    private readonly string _pipeName;

    private CancellationTokenSource? _lifetimeTokenSource;
    private Task? _connectionLoop;
    private JsonRpc? _jsonRpc;
    private IFireTowerService? _service;
    private ConnectionState _state = ConnectionState.Disconnected;

    public FireTowerIpcClient(ILogger<FireTowerIpcClient> logger, string? pipeName = null)
    {
        _logger = logger;
        _pipeName = pipeName ?? IpcDefaults.PipeName;
    }

    public ConnectionState State
    {
        get => _state;
        private set
        {
            if (_state == value)
            {
                return;
            }

            _state = value;
            ConnectionStateChanged?.Invoke(this, value);
        }
    }

    public IFireTowerService Service =>
        _service ?? throw new InvalidOperationException("Not connected to the FireTower service.");

    public event EventHandler<ConnectionState>? ConnectionStateChanged;
    public event EventHandler<VmStatusChangedEventDto>? VmStatusChanged;
    public event EventHandler? ConfigurationReloaded;
    public event EventHandler<ServiceStatusDto>? ServiceStatusChanged;
    public event EventHandler<ProviderInfoDto>? ProviderStatusChanged;

    public void Start()
    {
        if (_connectionLoop is not null)
        {
            return;
        }

        _lifetimeTokenSource = new CancellationTokenSource();
        _connectionLoop = ConnectionLoopAsync(_lifetimeTokenSource.Token);
    }

    public async Task StopAsync()
    {
        if (_lifetimeTokenSource is null)
        {
            return;
        }

        await _lifetimeTokenSource.CancelAsync().ConfigureAwait(false);
        if (_connectionLoop is not null)
        {
            // Started fire-and-forget from Start() (standard background-loop pattern).
            // VSTHRD003 targets Visual Studio's JoinableTaskFactory model, which this
            // WPF application does not use, so the deadlock it guards against cannot occur.
#pragma warning disable VSTHRD003
            await _connectionLoop.ConfigureAwait(false);
#pragma warning restore VSTHRD003
        }

        State = ConnectionState.Disconnected;
    }

    private async Task ConnectionLoopAsync(CancellationToken cancellationToken)
    {
        var attempt = 0;

        while (!cancellationToken.IsCancellationRequested)
        {
            State = attempt == 0 ? ConnectionState.Connecting : ConnectionState.Reconnecting;

            try
            {
                await using var stream = new NamedPipeClientStream(".", _pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
                await stream.ConnectAsync((int)TimeSpan.FromSeconds(5).TotalMilliseconds, cancellationToken).ConfigureAwait(false);

                _jsonRpc = new JsonRpc(stream);
                _jsonRpc.AddLocalRpcTarget<IFireTowerEventSink>(this, null);
                _service = _jsonRpc.Attach<IFireTowerService>();
                _jsonRpc.StartListening();

                State = ConnectionState.Connected;
                attempt = 0;
                _logger.LogInformation("Connected to FireTower service on pipe {PipeName}", _pipeName);

                // jsonRpc.Completion represents the connection's lifetime, not work
                // started elsewhere; see the StopAsync comment above re: VSTHRD003.
#pragma warning disable VSTHRD003
                await _jsonRpc.Completion.ConfigureAwait(false);
#pragma warning restore VSTHRD003
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "IPC connection attempt failed");
            }
            finally
            {
                _jsonRpc?.Dispose();
                _jsonRpc = null;
                _service = null;
            }

            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            State = ConnectionState.Reconnecting;
            attempt++;

            try
            {
                await Task.Delay(ReconnectInterval, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    public Task OnVmStatusChangedAsync(VmStatusChangedEventDto payload)
    {
        VmStatusChanged?.Invoke(this, payload);
        return Task.CompletedTask;
    }

    public Task OnConfigurationReloadedAsync()
    {
        ConfigurationReloaded?.Invoke(this, EventArgs.Empty);
        return Task.CompletedTask;
    }

    public Task OnServiceStatusChangedAsync(ServiceStatusDto status)
    {
        ServiceStatusChanged?.Invoke(this, status);
        return Task.CompletedTask;
    }

    public Task OnProviderStatusChangedAsync(ProviderInfoDto provider)
    {
        ProviderStatusChanged?.Invoke(this, provider);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _jsonRpc?.Dispose();
        _lifetimeTokenSource?.Dispose();
    }
}

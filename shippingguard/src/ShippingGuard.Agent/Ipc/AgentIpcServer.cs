using System.IO.Pipes;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using ShippingGuard.Core.Models;

namespace ShippingGuard.Agent.Ipc;

public sealed class AgentIpcServer : IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly string _pipeName;
    private readonly ILogger<AgentIpcServer> _logger;
    private readonly Func<IpcRequest, IpcResponse> _handler;
    private CancellationTokenSource? _cts;
    private Task? _loop;

    public AgentIpcServer(string pipeName, Func<IpcRequest, IpcResponse> handler, ILogger<AgentIpcServer> logger)
    {
        _pipeName = pipeName;
        _handler = handler;
        _logger = logger;
    }

    public void Start(CancellationToken stopping)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(stopping);
        _loop = AcceptLoopAsync(_cts.Token);
    }

    private async Task AcceptLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                var pipe = new NamedPipeServerStream(_pipeName, PipeDirection.InOut,
                    NamedPipeServerStream.MaxAllowedServerInstances, PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous);

                await pipe.WaitForConnectionAsync(ct);
                _ = HandleClientAsync(pipe, ct);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex) { _logger.LogError(ex, "IPC accept error."); await Task.Delay(2000, ct); }
        }
    }

    private async Task HandleClientAsync(NamedPipeServerStream pipe, CancellationToken ct)
    {
        await using var _ = pipe;
        try
        {
            using var reader = new StreamReader(pipe, leaveOpen: true);
            await using var writer = new StreamWriter(pipe, leaveOpen: true) { AutoFlush = true };

            var line = await reader.ReadLineAsync(ct);
            if (line is null) return;

            var request = JsonSerializer.Deserialize<IpcRequest>(line, JsonOptions);
            if (request is null) return;

            var response = _handler(request);
            await writer.WriteLineAsync(JsonSerializer.Serialize(response, JsonOptions));
        }
        catch (Exception ex) { _logger.LogDebug(ex, "IPC client error."); }
    }

    public void Dispose() => _cts?.Cancel();
}

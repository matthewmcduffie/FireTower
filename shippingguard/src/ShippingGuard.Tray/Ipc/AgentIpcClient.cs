using System.IO;
using System.IO.Pipes;
using System.Text.Json;
using System.Text.Json.Serialization;
using ShippingGuard.Core.Models;

namespace ShippingGuard.Tray.Ipc;

public sealed class AgentIpcClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly string _pipeName;

    public AgentIpcClient(string pipeName = "ShippingGuard.IPC")
    {
        _pipeName = pipeName;
    }

    public async Task<IpcResponse?> SendAsync(IpcRequest request, CancellationToken ct = default)
    {
        try
        {
            using var pipe = new NamedPipeClientStream(".", _pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
            await pipe.ConnectAsync(3000, ct);

            await using var writer = new StreamWriter(pipe, leaveOpen: true) { AutoFlush = true };
            using var reader = new StreamReader(pipe, leaveOpen: true);

            await writer.WriteLineAsync(JsonSerializer.Serialize(request, JsonOptions));
            var line = await reader.ReadLineAsync(ct);
            return line is null ? null : JsonSerializer.Deserialize<IpcResponse>(line, JsonOptions);
        }
        catch { return null; }
    }

    public Task<IpcResponse?> GetStatusAsync(CancellationToken ct = default) =>
        SendAsync(new IpcRequest { Command = IpcCommands.GetStatus }, ct);

    public Task<IpcResponse?> StartAppAsync(string profileId, CancellationToken ct = default) =>
        SendAsync(new IpcRequest { Command = IpcCommands.StartApp, ProfileId = profileId }, ct);

    public Task<IpcResponse?> StopAppAsync(string profileId, CancellationToken ct = default) =>
        SendAsync(new IpcRequest { Command = IpcCommands.StopApp, ProfileId = profileId }, ct);

    public Task<IpcResponse?> KillAppAsync(string profileId, CancellationToken ct = default) =>
        SendAsync(new IpcRequest { Command = IpcCommands.KillApp, ProfileId = profileId }, ct);

    public Task<IpcResponse?> SetMaintenanceAsync(string profileId, bool enabled, CancellationToken ct = default) =>
        SendAsync(new IpcRequest { Command = IpcCommands.SetMaintenance, ProfileId = profileId, Value = enabled }, ct);
}

namespace ShippingGuard.Core.Models;

public sealed class IpcRequest
{
    public required string Command { get; init; }
    public string? ProfileId { get; init; }
    public bool? Value { get; init; }
}

public sealed class IpcResponse
{
    public bool Success { get; init; }
    public string? Error { get; init; }
    public IReadOnlyList<AppStatus>? Statuses { get; init; }

    public static IpcResponse Ok(IReadOnlyList<AppStatus>? statuses = null) =>
        new() { Success = true, Statuses = statuses };

    public static IpcResponse Fail(string error) =>
        new() { Success = false, Error = error };
}

public static class IpcCommands
{
    public const string GetStatus = "GetStatus";
    public const string StartApp = "StartApp";
    public const string StopApp = "StopApp";
    public const string KillApp = "KillApp";
    public const string SetMaintenance = "SetMaintenance";
    public const string SetEnabled = "SetEnabled";
}

namespace FireTower.Shared.Contracts;

/// <summary>
/// Standard result envelope for every IPC response and most internal service operations.
/// Mirrors the message format defined in ipc.md: a success flag, an error code, an error
/// message, and a payload. Never carries raw exceptions across a boundary.
/// </summary>
public sealed class OperationResult<T>
{
    public bool Success { get; }
    public string? ErrorCode { get; }
    public string? ErrorMessage { get; }
    public T? Payload { get; }

    // Public rather than private: this is the only constructor, and StreamJsonRpc's
    // Newtonsoft.Json-based formatter needs an accessible constructor to deserialize
    // responses crossing the IPC boundary.
    public OperationResult(bool success, T? payload, string? errorCode, string? errorMessage)
    {
        Success = success;
        Payload = payload;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
    }

    public static OperationResult<T> Ok(T payload) => new(true, payload, null, null);

    public static OperationResult<T> Fail(string errorCode, string errorMessage) =>
        new(false, default, errorCode, errorMessage);
}

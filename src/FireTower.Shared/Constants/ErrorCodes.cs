namespace FireTower.Shared.Constants;

/// <summary>
/// Deterministic error codes returned in <see cref="Contracts.OperationResult{T}"/> responses,
/// matching the error categories defined in ipc.md.
/// </summary>
public static class ErrorCodes
{
    public const string InvalidRequest = "INVALID_REQUEST";
    public const string InvalidConfiguration = "INVALID_CONFIGURATION";
    public const string ProviderUnavailable = "PROVIDER_UNAVAILABLE";
    public const string VirtualMachineNotFound = "VM_NOT_FOUND";
    public const string PermissionDenied = "PERMISSION_DENIED";
    public const string Timeout = "TIMEOUT";
    public const string InternalServiceError = "INTERNAL_SERVICE_ERROR";
    public const string UnsupportedOperation = "UNSUPPORTED_OPERATION";
}

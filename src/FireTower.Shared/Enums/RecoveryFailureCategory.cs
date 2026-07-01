namespace FireTower.Shared.Enums;

/// <summary>
/// Classification of why a recovery action did not succeed, recorded in restart history.
/// </summary>
public enum RecoveryFailureCategory
{
    None,
    HealthFailure,
    ProviderFailure,
    Timeout,
    PowerOperationFailed,
    ConfigurationError,
    UnknownError,
}

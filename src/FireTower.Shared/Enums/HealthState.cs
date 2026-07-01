namespace FireTower.Shared.Enums;

/// <summary>
/// Overall health state of a monitored virtual machine, as produced by the Health Engine.
/// Independent of provider-specific power states.
/// </summary>
public enum HealthState
{
    Unknown,
    Healthy,
    Warning,
    Degraded,
    Critical,
    Recovering,
    Offline,
}

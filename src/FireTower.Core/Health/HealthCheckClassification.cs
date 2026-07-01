namespace FireTower.Core.Health;

/// <summary>
/// Where a single health check sits relative to its configured failure and recovery
/// thresholds, after applying hysteresis: a check that has crossed its failure threshold
/// stays classified as <see cref="Failing"/> until it crosses the recovery threshold,
/// passing through <see cref="Recovering"/> in between.
/// </summary>
public enum HealthCheckClassification
{
    Passing,
    Recovering,
    Failing,
}

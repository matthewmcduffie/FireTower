using System.Diagnostics;
using FireTower.Core.Interfaces;
using FireTower.Core.Models;
using FireTower.Shared.Enums;

namespace FireTower.Core.Health.Checks;

/// <summary>
/// Verifies that the VM is reachable by its provider AND is in a running state.
/// A VM that is stopped, paused, saved, or aborted is reported as Failed so the
/// Restart Engine can bring it back. Transitional states (Starting, Stopping,
/// Restoring) are treated leniently so the engine doesn't interfere mid-transition.
/// </summary>
public sealed class ProviderStatusHealthCheck : IHealthCheck
{
    public HealthCheckKind Kind => HealthCheckKind.ProviderStatus;

    private readonly IProviderManager _providerManager;

    public ProviderStatusHealthCheck(IProviderManager providerManager)
    {
        _providerManager = providerManager;
    }

    public async Task<HealthCheckResult> ExecuteAsync(
        VirtualMachine virtualMachine,
        HealthCheckDefinition definition,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        if (!_providerManager.TryGetProvider(virtualMachine.ProviderId, out var provider) || provider is null)
        {
            return HealthCheckResultFactory.Create(
                definition.Id, HealthCheckOutcome.Failed, stopwatch.Elapsed,
                $"Provider '{virtualMachine.ProviderId}' is unavailable.");
        }

        VmPowerState state;
        try
        {
            state = await provider.GetStateAsync(virtualMachine.ExternalId, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return HealthCheckResultFactory.Create(
                definition.Id, HealthCheckOutcome.Timeout, stopwatch.Elapsed,
                "Provider status check timed out.");
        }
        catch (Exception ex)
        {
            return HealthCheckResultFactory.Create(
                definition.Id, HealthCheckOutcome.Failed, stopwatch.Elapsed, ex.Message);
        }

        // Map VirtualBox power states to health outcomes.
        // Running/Starting/Restoring are healthy — don't interrupt a VM that is
        // coming up or being restored.
        // Stopping is a transient Warning — it won't increment the failure counter
        // so the engine won't restart a VM that the user is deliberately shutting down.
        // Everything else (Stopped, Paused, Saved, Aborted, Inaccessible, Unknown)
        // is a hard failure that will trigger the Restart Engine once the failure
        // threshold is crossed.
        var outcome = state switch
        {
            VmPowerState.Running    => HealthCheckOutcome.Healthy,
            VmPowerState.Starting   => HealthCheckOutcome.Healthy,
            VmPowerState.Restoring  => HealthCheckOutcome.Healthy,
            VmPowerState.Stopping   => HealthCheckOutcome.Warning,
            _                       => HealthCheckOutcome.Failed,
        };

        return HealthCheckResultFactory.Create(
            definition.Id, outcome, stopwatch.Elapsed, state.ToString());
    }
}

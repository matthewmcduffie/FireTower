using System.Diagnostics;
using FireTower.Core.Interfaces;
using FireTower.Core.Models;
using FireTower.Core.Utilities;
using FireTower.Shared.Enums;
using Microsoft.Extensions.Logging;

namespace FireTower.Core.Restart;

/// <summary>
/// Default <see cref="IRestartEngine"/> implementation. Converts health evaluations into
/// recovery decisions and executes them, following the Recovery Flow, Safety Rules, and
/// escalation behavior described in restart-engine.md. Never performs health checks itself.
/// </summary>
public sealed class RestartEngine : IRestartEngine
{
    private readonly IRestartHistoryRepository _restartHistory;
    private readonly IProviderManager _providerManager;
    private readonly RestartLockRegistry _lockRegistry;
    private readonly IClock _clock;
    private readonly ILogger<RestartEngine> _logger;

    public RestartEngine(
        IRestartHistoryRepository restartHistory,
        IProviderManager providerManager,
        RestartLockRegistry lockRegistry,
        IClock clock,
        ILogger<RestartEngine> logger)
    {
        _restartHistory = restartHistory;
        _providerManager = providerManager;
        _lockRegistry = lockRegistry;
        _clock = clock;
        _logger = logger;
    }

    public async Task<RecoveryDecision> EvaluateAsync(VirtualMachine virtualMachine, RecoveryProfile profile, HealthEvaluation evaluation, CancellationToken cancellationToken)
    {
        // Never restart a healthy VM (restart-engine.md, Safety Rules).
        if (evaluation.NewState is HealthState.Healthy or HealthState.Unknown or HealthState.Recovering)
        {
            return DoNothing(virtualMachine.Id, "Virtual machine is healthy or still settling; no recovery action required.");
        }

        if (evaluation.NewState == HealthState.Warning)
        {
            return new RecoveryDecision { VirtualMachineId = virtualMachine.Id, Action = RecoveryAction.Notify, Reason = "Health degraded to Warning." };
        }

        var now = _clock.UtcNow;

        if (virtualMachine.LastRestart is { } lastRestart && now - lastRestart < TimeSpan.FromSeconds(profile.CooldownSeconds))
        {
            return Suppressed(virtualMachine.Id, "Cooldown period has not elapsed since the last restart.");
        }

        if (IsWithinMaintenanceWindow(profile, now))
        {
            return Suppressed(virtualMachine.Id, "Recovery suppressed during the configured maintenance window.");
        }

        var attemptCount = await _restartHistory.CountWithinWindowAsync(
            virtualMachine.Id, now - TimeSpan.FromSeconds(profile.RetryWindowSeconds), cancellationToken).ConfigureAwait(false);

        if (attemptCount >= profile.MaxRestartAttempts)
        {
            return new RecoveryDecision
            {
                VirtualMachineId = virtualMachine.Id,
                Action = RecoveryAction.Notify,
                Reason = $"Retry limit of {profile.MaxRestartAttempts} restarts within {profile.RetryWindowSeconds}s exceeded.",
            };
        }

        var sequence = profile.EscalationSequence.Count > 0
            ? profile.EscalationSequence
            : new[] { RecoveryAction.Restart };

        var action = sequence[Math.Min(attemptCount, sequence.Count - 1)];

        return new RecoveryDecision
        {
            VirtualMachineId = virtualMachine.Id,
            Action = action,
            Reason = $"Health state is {evaluation.NewState}; escalation step {attemptCount + 1}.",
        };
    }

    public async Task<RecoveryResult> ExecuteAsync(VirtualMachine virtualMachine, RecoveryDecision decision, CancellationToken cancellationToken)
    {
        var correlationId = CorrelationId.New();

        if (decision.Suppressed || decision.Action is RecoveryAction.DoNothing or RecoveryAction.LogOnly)
        {
            return new RecoveryResult
            {
                VirtualMachineId = virtualMachine.Id,
                Action = decision.Action,
                Success = true,
                Duration = TimeSpan.Zero,
                Timestamp = _clock.UtcNow,
                CorrelationId = correlationId,
            };
        }

        using var _ = await _lockRegistry.AcquireAsync(virtualMachine.Id, cancellationToken).ConfigureAwait(false);

        var stopwatch = Stopwatch.StartNew();
        try
        {
            if (decision.Action != RecoveryAction.Notify)
            {
                var provider = _providerManager.GetProvider(virtualMachine.ProviderId);
                await DispatchAsync(provider, virtualMachine.ExternalId, decision.Action, cancellationToken).ConfigureAwait(false);
            }

            var result = new RecoveryResult
            {
                VirtualMachineId = virtualMachine.Id,
                Action = decision.Action,
                Success = true,
                Duration = stopwatch.Elapsed,
                Timestamp = _clock.UtcNow,
                CorrelationId = correlationId,
            };

            await _restartHistory.RecordAsync(result, cancellationToken).ConfigureAwait(false);
            return result;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return await RecordFailureAsync(virtualMachine.Id, decision.Action, RecoveryFailureCategory.Timeout, "Operation timed out.", stopwatch.Elapsed, correlationId, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Recovery action {Action} failed for virtual machine {VirtualMachineId}", decision.Action, virtualMachine.Id);
            return await RecordFailureAsync(virtualMachine.Id, decision.Action, RecoveryFailureCategory.ProviderFailure, ex.Message, stopwatch.Elapsed, correlationId, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task<RecoveryResult> RecordFailureAsync(
        Guid virtualMachineId, RecoveryAction action, RecoveryFailureCategory category, string reason, TimeSpan duration, string correlationId, CancellationToken cancellationToken)
    {
        var result = new RecoveryResult
        {
            VirtualMachineId = virtualMachineId,
            Action = action,
            Success = false,
            FailureCategory = category,
            FailureReason = reason,
            Duration = duration,
            Timestamp = _clock.UtcNow,
            CorrelationId = correlationId,
        };

        await _restartHistory.RecordAsync(result, cancellationToken).ConfigureAwait(false);
        return result;
    }

    private static Task DispatchAsync(IVmProvider provider, string externalId, RecoveryAction action, CancellationToken cancellationToken) => action switch
    {
        RecoveryAction.GracefulShutdown => provider.GracefulShutdownAsync(externalId, cancellationToken),
        RecoveryAction.Restart => provider.RestartAsync(externalId, cancellationToken),
        RecoveryAction.ForceRestart => provider.ForceRestartAsync(externalId, cancellationToken),
        RecoveryAction.PowerOff => provider.PowerOffAsync(externalId, cancellationToken),
        RecoveryAction.Start => provider.StartAsync(externalId, cancellationToken),
        _ => Task.CompletedTask,
    };

    private static bool IsWithinMaintenanceWindow(RecoveryProfile profile, DateTimeOffset now)
    {
        if (profile.MaintenanceWindowDay is not { } day ||
            profile.MaintenanceWindowStart is not { } start ||
            profile.MaintenanceWindowEnd is not { } end)
        {
            return false;
        }

        if (now.DayOfWeek != day)
        {
            return false;
        }

        var timeOfDay = TimeOnly.FromTimeSpan(now.TimeOfDay);
        return timeOfDay >= start && timeOfDay <= end;
    }

    private static RecoveryDecision DoNothing(Guid virtualMachineId, string reason) => new()
    {
        VirtualMachineId = virtualMachineId,
        Action = RecoveryAction.DoNothing,
        Reason = reason,
    };

    private static RecoveryDecision Suppressed(Guid virtualMachineId, string reason) => new()
    {
        VirtualMachineId = virtualMachineId,
        Action = RecoveryAction.DoNothing,
        Reason = reason,
        Suppressed = true,
        SuppressionReason = reason,
    };
}

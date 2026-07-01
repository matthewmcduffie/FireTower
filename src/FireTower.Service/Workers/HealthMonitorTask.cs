using FireTower.Core.Interfaces;
using FireTower.Core.Scheduling;
using FireTower.Core.Services;
using FireTower.Core.Utilities;
using FireTower.Shared.Enums;
using Microsoft.Extensions.Logging;
using IConfigurationManager = FireTower.Core.Interfaces.IConfigurationManager;

namespace FireTower.Service.Workers;

/// <summary>
/// The core monitoring loop described in service.md: for every enabled virtual machine,
/// run its configured health checks, then hand the result to the Restart Engine. Each VM
/// is evaluated independently so a slow VM never blocks the others.
/// </summary>
public sealed class HealthMonitorTask : IScheduledTask
{
    public string Name => "Health Monitor";
    public TimeSpan Interval { get; }

    private readonly IVirtualMachineRepository _vmRepository;
    private readonly IConfigurationManager _configurationManager;
    private readonly IHealthEngine _healthEngine;
    private readonly IRestartEngine _restartEngine;
    private readonly IHealthHistoryRepository _healthHistory;
    private readonly IEventPublisher _eventPublisher;
    private readonly MonitoringState _monitoringState;
    private readonly ILogger<HealthMonitorTask> _logger;

    public HealthMonitorTask(
        IVirtualMachineRepository vmRepository,
        IConfigurationManager configurationManager,
        IHealthEngine healthEngine,
        IRestartEngine restartEngine,
        IHealthHistoryRepository healthHistory,
        IEventPublisher eventPublisher,
        MonitoringState monitoringState,
        ILogger<HealthMonitorTask> logger)
    {
        _vmRepository = vmRepository;
        _configurationManager = configurationManager;
        _healthEngine = healthEngine;
        _restartEngine = restartEngine;
        _healthHistory = healthHistory;
        _eventPublisher = eventPublisher;
        _monitoringState = monitoringState;
        _logger = logger;
        Interval = TimeSpan.FromSeconds(Math.Max(5, configurationManager.Current.Global.DefaultPollingIntervalSeconds));
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        if (_monitoringState.IsPaused)
        {
            return;
        }

        var virtualMachines = await _vmRepository.GetAllAsync(cancellationToken).ConfigureAwait(false);
        var configuration = _configurationManager.Current;

        var evaluations = virtualMachines
            .Where(vm => vm.Enabled)
            .Select(vm => EvaluateOneAsync(vm, configuration, cancellationToken));

        await Task.WhenAll(evaluations).ConfigureAwait(false);
    }

    private async Task EvaluateOneAsync(
        Core.Models.VirtualMachine virtualMachine,
        Core.Configuration.FireTowerConfiguration configuration,
        CancellationToken cancellationToken)
    {
        try
        {
            var healthProfile = configuration.HealthProfiles.FirstOrDefault(p => p.Id == virtualMachine.HealthProfileId);
            if (healthProfile is null)
            {
                _logger.LogWarning("Virtual machine {Name} references unknown Health Profile {ProfileId}", virtualMachine.Name, virtualMachine.HealthProfileId);
                return;
            }

            var evaluation = await _healthEngine.EvaluateAsync(virtualMachine, healthProfile, cancellationToken).ConfigureAwait(false);
            await _healthHistory.RecordAsync(evaluation, cancellationToken).ConfigureAwait(false);

            virtualMachine.Health = evaluation.NewState;
            virtualMachine.LastHealthCheck = evaluation.Timestamp;

            // Extract the current power state from the ProviderStatus check result.
            // ProviderStatusHealthCheck stores the VmPowerState enum name in Detail.
            var providerResult = evaluation.CheckResults
                .FirstOrDefault(r => r.HealthCheckId == "provider-status");
            if (providerResult?.Detail is string stateText &&
                Enum.TryParse<Shared.Enums.VmPowerState>(stateText, out var observedState))
            {
                virtualMachine.PowerState = observedState;
            }

            await _vmRepository.UpsertAsync(virtualMachine, cancellationToken).ConfigureAwait(false);

            if (evaluation.StateChanged)
            {
                _eventPublisher.PublishOperationalEvent(
                    "Health",
                    $"{virtualMachine.Name} health changed from {evaluation.PreviousState} to {evaluation.NewState}.",
                    virtualMachine.Id,
                    CorrelationId.New());
            }

            _eventPublisher.PublishVmStatusChanged(virtualMachine);

            if (evaluation.NewState is HealthState.Healthy or HealthState.Unknown or HealthState.Recovering)
            {
                return;
            }

            var recoveryProfile = configuration.RecoveryProfiles.FirstOrDefault(p => p.Id == virtualMachine.RecoveryProfileId);
            if (recoveryProfile is null)
            {
                _logger.LogWarning("Virtual machine {Name} references unknown Recovery Profile {ProfileId}", virtualMachine.Name, virtualMachine.RecoveryProfileId);
                return;
            }

            var decision = await _restartEngine.EvaluateAsync(virtualMachine, recoveryProfile, evaluation, cancellationToken).ConfigureAwait(false);
            var result = await _restartEngine.ExecuteAsync(virtualMachine, decision, cancellationToken).ConfigureAwait(false);

            if (result.Action is RecoveryAction.Restart or RecoveryAction.ForceRestart && result.Success)
            {
                virtualMachine.RestartCount++;
                virtualMachine.LastRestart = result.Timestamp;
                virtualMachine.RecoveryState = RecoveryState.Succeeded;
                await _vmRepository.UpsertAsync(virtualMachine, cancellationToken).ConfigureAwait(false);
            }

            _eventPublisher.PublishOperationalEvent(
                "Restart",
                $"{virtualMachine.Name}: {decision.Action} ({(result.Success ? "succeeded" : "failed: " + result.FailureReason)}).",
                virtualMachine.Id,
                result.CorrelationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health monitoring cycle failed for virtual machine {Name}", virtualMachine.Name);
        }
    }
}

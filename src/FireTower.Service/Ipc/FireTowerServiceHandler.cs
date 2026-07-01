using FireTower.Core.Health;
using FireTower.Core.Interfaces;
using FireTower.Core.Services;
using FireTower.Service.Logging;
using FireTower.Shared.Constants;
using FireTower.Shared.Contracts;
using FireTower.Shared.DTOs;
using FireTower.Shared.Enums;
using FireTower.Shared.Exceptions;
using Microsoft.Extensions.Logging;
using IConfigurationManager = FireTower.Core.Interfaces.IConfigurationManager;

namespace FireTower.Service.Ipc;

/// <summary>
/// Server-side implementation of <see cref="IFireTowerService"/>. Every request is
/// validated and every response goes through <see cref="OperationResult{T}"/> so raw
/// exceptions never cross the IPC boundary, per ipc.md.
/// </summary>
public sealed class FireTowerServiceHandler : IFireTowerService
{
    private readonly IVirtualMachineRepository _vmRepository;
    private readonly IConfigurationManager _configurationManager;
    private readonly IProviderManager _providerManager;
    private readonly IEventRepository _eventRepository;
    private readonly IStatisticsRepository _statisticsRepository;
    private readonly InMemoryLogStore _logStore;
    private readonly MonitoringState _monitoringState;
    private readonly IReadOnlyDictionary<HealthCheckKind, IHealthCheck> _healthChecksByKind;
    private readonly HealthCheckExecutor _healthCheckExecutor;
    private readonly ILogger<FireTowerServiceHandler> _logger;

    public FireTowerServiceHandler(
        IVirtualMachineRepository vmRepository,
        IConfigurationManager configurationManager,
        IProviderManager providerManager,
        IEventRepository eventRepository,
        IStatisticsRepository statisticsRepository,
        InMemoryLogStore logStore,
        MonitoringState monitoringState,
        IEnumerable<IHealthCheck> healthChecks,
        HealthCheckExecutor healthCheckExecutor,
        ILogger<FireTowerServiceHandler> logger)
    {
        _vmRepository = vmRepository;
        _configurationManager = configurationManager;
        _providerManager = providerManager;
        _eventRepository = eventRepository;
        _statisticsRepository = statisticsRepository;
        _logStore = logStore;
        _monitoringState = monitoringState;
        _healthChecksByKind = healthChecks.ToDictionary(c => c.Kind);
        _healthCheckExecutor = healthCheckExecutor;
        _logger = logger;
    }

    public async Task<OperationResult<ServiceStatusDto>> GetServiceStatusAsync(CancellationToken cancellationToken)
    {
        var vms = await _vmRepository.GetAllAsync(cancellationToken).ConfigureAwait(false);

        var dto = new ServiceStatusDto
        {
            State = _monitoringState.CurrentServiceState,
            StartedAt = _monitoringState.StartedAt,
            MonitoredVmCount = vms.Count,
            HealthyVmCount = vms.Count(v => v.Health == HealthState.Healthy),
            WarningVmCount = vms.Count(v => v.Health == HealthState.Warning),
            FailedVmCount = vms.Count(v => v.Health is HealthState.Critical or HealthState.Offline or HealthState.Degraded),
            Version = typeof(FireTowerServiceHandler).Assembly.GetName().Version?.ToString() ?? "0.0.0",
        };

        return OperationResult<ServiceStatusDto>.Ok(dto);
    }

    public async Task<OperationResult<IReadOnlyList<VirtualMachineDto>>> GetVirtualMachinesAsync(CancellationToken cancellationToken)
    {
        var vms = await _vmRepository.GetAllAsync(cancellationToken).ConfigureAwait(false);
        return OperationResult<IReadOnlyList<VirtualMachineDto>>.Ok(vms.Select(DtoMapping.ToDto).ToList());
    }

    public async Task<OperationResult<VirtualMachineDto>> GetVirtualMachineDetailsAsync(Guid id, CancellationToken cancellationToken)
    {
        var vm = await _vmRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        return vm is null
            ? OperationResult<VirtualMachineDto>.Fail(ErrorCodes.VirtualMachineNotFound, $"Virtual machine '{id}' was not found.")
            : OperationResult<VirtualMachineDto>.Ok(DtoMapping.ToDto(vm));
    }

    public async Task<OperationResult<IReadOnlyList<DiscoveredVirtualMachineDto>>> DiscoverVirtualMachinesAsync(string? providerId, CancellationToken cancellationToken)
    {
        var providers = providerId is null
            ? _providerManager.GetRegisteredProviders().Where(p => p.IsAvailable)
            : _providerManager.GetRegisteredProviders().Where(p => p.IsAvailable && p.ProviderId == providerId);

        var existing = await _vmRepository.GetAllAsync(cancellationToken).ConfigureAwait(false);
        var results = new List<DiscoveredVirtualMachineDto>();

        foreach (var registration in providers)
        {
            if (!_providerManager.TryGetProvider(registration.ProviderId, out var provider) || provider is null)
            {
                continue;
            }

            var discovered = await provider.DiscoverVirtualMachinesAsync(cancellationToken).ConfigureAwait(false);
            results.AddRange(discovered.Select(d => DtoMapping.ToDto(
                d, existing.Any(e => e.ProviderId == d.ProviderId && e.ExternalId == d.ExternalId))));
        }

        return OperationResult<IReadOnlyList<DiscoveredVirtualMachineDto>>.Ok(results);
    }

    public Task<OperationResult<Unit>> StartVirtualMachineAsync(Guid id, CancellationToken cancellationToken) =>
        ExecutePowerOperationAsync(id, (provider, externalId, ct) => provider.StartAsync(externalId, ct), cancellationToken);

    public Task<OperationResult<Unit>> StopVirtualMachineAsync(Guid id, CancellationToken cancellationToken) =>
        ExecutePowerOperationAsync(id, (provider, externalId, ct) => provider.StopAsync(externalId, ct), cancellationToken);

    public Task<OperationResult<Unit>> RestartVirtualMachineAsync(Guid id, CancellationToken cancellationToken) =>
        ExecutePowerOperationAsync(id, (provider, externalId, ct) => provider.RestartAsync(externalId, ct), cancellationToken);

    public Task<OperationResult<Unit>> ForceRestartVirtualMachineAsync(Guid id, CancellationToken cancellationToken) =>
        ExecutePowerOperationAsync(id, (provider, externalId, ct) => provider.ForceRestartAsync(externalId, ct), cancellationToken);

    public async Task<OperationResult<Unit>> SetMonitoringEnabledAsync(Guid id, bool enabled, CancellationToken cancellationToken)
    {
        var vm = await _vmRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (vm is null)
        {
            return OperationResult<Unit>.Fail(ErrorCodes.VirtualMachineNotFound, $"Virtual machine '{id}' was not found.");
        }

        vm.Enabled = enabled;
        vm.DateModified = DateTimeOffset.UtcNow;
        await _vmRepository.UpsertAsync(vm, cancellationToken).ConfigureAwait(false);
        return OperationResult<Unit>.Ok(Unit.Value);
    }

    public async Task<OperationResult<HealthCheckOutcomeDto>> RunHealthCheckAsync(Guid id, string healthCheckId, CancellationToken cancellationToken)
    {
        var vm = await _vmRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (vm is null)
        {
            return OperationResult<HealthCheckOutcomeDto>.Fail(ErrorCodes.VirtualMachineNotFound, $"Virtual machine '{id}' was not found.");
        }

        var profile = _configurationManager.Current.HealthProfiles.FirstOrDefault(p => p.Id == vm.HealthProfileId);
        var definition = profile?.Checks.FirstOrDefault(c => c.Id == healthCheckId);
        if (definition is null)
        {
            return OperationResult<HealthCheckOutcomeDto>.Fail(ErrorCodes.InvalidRequest, $"Health check '{healthCheckId}' is not configured for this virtual machine.");
        }

        if (!_healthChecksByKind.TryGetValue(definition.Kind, out var check))
        {
            return OperationResult<HealthCheckOutcomeDto>.Fail(ErrorCodes.UnsupportedOperation, $"No implementation registered for {definition.Kind}.");
        }

        var result = await _healthCheckExecutor.ExecuteWithRetriesAsync(check, vm, definition, cancellationToken).ConfigureAwait(false);
        return OperationResult<HealthCheckOutcomeDto>.Ok(new HealthCheckOutcomeDto
        {
            HealthCheckId = result.HealthCheckId,
            Outcome = result.Outcome,
            Duration = result.Duration,
            Detail = result.Detail,
        });
    }

    public Task<OperationResult<IReadOnlyList<ProviderInfoDto>>> GetProvidersAsync(CancellationToken cancellationToken) =>
        Task.FromResult(OperationResult<IReadOnlyList<ProviderInfoDto>>.Ok(
            _providerManager.GetRegisteredProviders().Select(DtoMapping.ToDto).ToList()));

    public Task<OperationResult<IReadOnlyList<LogEntryDto>>> GetLogsAsync(LogQueryDto query, CancellationToken cancellationToken) =>
        Task.FromResult(OperationResult<IReadOnlyList<LogEntryDto>>.Ok(_logStore.Query(query)));

    public async Task<OperationResult<IReadOnlyList<EventRecordDto>>> GetEventsAsync(int skip, int take, CancellationToken cancellationToken)
    {
        var events = await _eventRepository.GetRecentAsync(skip, take, cancellationToken).ConfigureAwait(false);
        return OperationResult<IReadOnlyList<EventRecordDto>>.Ok(events.Select(DtoMapping.ToDto).ToList());
    }

    public async Task<OperationResult<StatisticsSnapshotDto>> GetStatisticsAsync(CancellationToken cancellationToken)
    {
        var snapshot = await _statisticsRepository.GetLatestAsync(cancellationToken).ConfigureAwait(false);
        var uptime = DateTimeOffset.UtcNow - _monitoringState.StartedAt;

        if (snapshot is null)
        {
            return OperationResult<StatisticsSnapshotDto>.Fail(ErrorCodes.InternalServiceError, "No statistics have been recorded yet.");
        }

        return OperationResult<StatisticsSnapshotDto>.Ok(DtoMapping.ToDto(snapshot, uptime));
    }

    public async Task<OperationResult<string>> GetConfigurationAsync(CancellationToken cancellationToken)
    {
        var json = await _configurationManager.ExportAsync(cancellationToken).ConfigureAwait(false);
        return OperationResult<string>.Ok(json);
    }

    public async Task<OperationResult<Unit>> SaveConfigurationAsync(string json, CancellationToken cancellationToken)
    {
        try
        {
            await _configurationManager.ImportAsync(json, cancellationToken).ConfigureAwait(false);
            return OperationResult<Unit>.Ok(Unit.Value);
        }
        catch (ValidationException ex)
        {
            return OperationResult<Unit>.Fail(ErrorCodes.InvalidConfiguration, ex.Message);
        }
        catch (ConfigurationException ex)
        {
            return OperationResult<Unit>.Fail(ErrorCodes.InvalidConfiguration, ex.Message);
        }
    }

    public async Task<OperationResult<Unit>> ReloadConfigurationAsync(CancellationToken cancellationToken)
    {
        await _configurationManager.ReloadAsync(cancellationToken).ConfigureAwait(false);
        return OperationResult<Unit>.Ok(Unit.Value);
    }

    public Task<OperationResult<Unit>> PauseMonitoringAsync(CancellationToken cancellationToken)
    {
        _monitoringState.Pause();
        return Task.FromResult(OperationResult<Unit>.Ok(Unit.Value));
    }

    public Task<OperationResult<Unit>> ResumeMonitoringAsync(CancellationToken cancellationToken)
    {
        _monitoringState.Resume();
        return Task.FromResult(OperationResult<Unit>.Ok(Unit.Value));
    }

    private async Task<OperationResult<Unit>> ExecutePowerOperationAsync(
        Guid id, Func<IVmProvider, string, CancellationToken, Task> operation, CancellationToken cancellationToken)
    {
        var vm = await _vmRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (vm is null)
        {
            return OperationResult<Unit>.Fail(ErrorCodes.VirtualMachineNotFound, $"Virtual machine '{id}' was not found.");
        }

        if (!_providerManager.TryGetProvider(vm.ProviderId, out var provider) || provider is null)
        {
            return OperationResult<Unit>.Fail(ErrorCodes.ProviderUnavailable, $"Provider '{vm.ProviderId}' is unavailable.");
        }

        try
        {
            await operation(provider, vm.ExternalId, cancellationToken).ConfigureAwait(false);
            return OperationResult<Unit>.Ok(Unit.Value);
        }
        catch (ProviderException ex)
        {
            _logger.LogWarning(ex, "Manual power operation failed for virtual machine {Name}", vm.Name);
            return OperationResult<Unit>.Fail(ErrorCodes.InternalServiceError, ex.Message);
        }
    }
}

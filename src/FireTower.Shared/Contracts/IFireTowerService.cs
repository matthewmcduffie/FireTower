using FireTower.Shared.DTOs;

namespace FireTower.Shared.Contracts;

/// <summary>
/// The full set of requests a client may send to the FireTower Windows Service over the
/// Named Pipe transport, matching the Request Types catalog in ipc.md. The service hosts
/// an implementation of this interface as a StreamJsonRpc target; clients obtain a
/// strongly-typed proxy via <c>JsonRpc.Attach&lt;IFireTowerService&gt;</c>.
/// </summary>
public interface IFireTowerService
{
    Task<OperationResult<ServiceStatusDto>> GetServiceStatusAsync(CancellationToken cancellationToken);

    Task<OperationResult<IReadOnlyList<VirtualMachineDto>>> GetVirtualMachinesAsync(CancellationToken cancellationToken);

    Task<OperationResult<VirtualMachineDto>> GetVirtualMachineDetailsAsync(Guid id, CancellationToken cancellationToken);

    Task<OperationResult<IReadOnlyList<DiscoveredVirtualMachineDto>>> DiscoverVirtualMachinesAsync(string? providerId, CancellationToken cancellationToken);

    Task<OperationResult<Unit>> StartVirtualMachineAsync(Guid id, CancellationToken cancellationToken);

    Task<OperationResult<Unit>> StopVirtualMachineAsync(Guid id, CancellationToken cancellationToken);

    Task<OperationResult<Unit>> RestartVirtualMachineAsync(Guid id, CancellationToken cancellationToken);

    Task<OperationResult<Unit>> ForceRestartVirtualMachineAsync(Guid id, CancellationToken cancellationToken);

    Task<OperationResult<Unit>> SetMonitoringEnabledAsync(Guid id, bool enabled, CancellationToken cancellationToken);

    Task<OperationResult<HealthCheckOutcomeDto>> RunHealthCheckAsync(Guid id, string healthCheckId, CancellationToken cancellationToken);

    Task<OperationResult<IReadOnlyList<ProviderInfoDto>>> GetProvidersAsync(CancellationToken cancellationToken);

    Task<OperationResult<IReadOnlyList<LogEntryDto>>> GetLogsAsync(LogQueryDto query, CancellationToken cancellationToken);

    Task<OperationResult<IReadOnlyList<EventRecordDto>>> GetEventsAsync(int skip, int take, CancellationToken cancellationToken);

    Task<OperationResult<StatisticsSnapshotDto>> GetStatisticsAsync(CancellationToken cancellationToken);

    Task<OperationResult<string>> GetConfigurationAsync(CancellationToken cancellationToken);

    Task<OperationResult<Unit>> SaveConfigurationAsync(string json, CancellationToken cancellationToken);

    Task<OperationResult<Unit>> ReloadConfigurationAsync(CancellationToken cancellationToken);

    Task<OperationResult<Unit>> PauseMonitoringAsync(CancellationToken cancellationToken);

    Task<OperationResult<Unit>> ResumeMonitoringAsync(CancellationToken cancellationToken);
}

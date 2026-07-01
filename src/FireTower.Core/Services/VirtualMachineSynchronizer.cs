using FireTower.Core.Configuration;
using FireTower.Core.Interfaces;
using FireTower.Core.Models;
using Microsoft.Extensions.Logging;

namespace FireTower.Core.Services;

/// <summary>
/// Reconciles the monitored-VM list in configuration with FireTower's persisted view of
/// those machines (database.md's VirtualMachines table represents "FireTower's view of
/// monitored machines"). Runs at startup and after every configuration reload.
/// </summary>
public sealed class VirtualMachineSynchronizer
{
    private readonly IVirtualMachineRepository _repository;
    private readonly IClock _clock;
    private readonly ILogger<VirtualMachineSynchronizer> _logger;

    public VirtualMachineSynchronizer(IVirtualMachineRepository repository, IClock clock, ILogger<VirtualMachineSynchronizer> logger)
    {
        _repository = repository;
        _clock = clock;
        _logger = logger;
    }

    public async Task SynchronizeAsync(FireTowerConfiguration configuration, CancellationToken cancellationToken)
    {
        var existing = (await _repository.GetAllAsync(cancellationToken).ConfigureAwait(false))
            .ToDictionary(vm => vm.Id);

        var configuredIds = new HashSet<Guid>();

        foreach (var configEntry in configuration.VirtualMachines)
        {
            configuredIds.Add(configEntry.Id);
            var now = _clock.UtcNow;

            if (existing.TryGetValue(configEntry.Id, out var current))
            {
                var updated = new VirtualMachine
                {
                    Id = current.Id,
                    ExternalId = configEntry.ExternalId,
                    Name = configEntry.Name,
                    ProviderId = configEntry.ProviderId,
                    Enabled = configEntry.Enabled,
                    HealthProfileId = configEntry.HealthProfileId,
                    RecoveryProfileId = configEntry.RecoveryProfileId,
                    Tags = configEntry.Tags,
                    PowerState = current.PowerState,
                    Health = current.Health,
                    RecoveryState = current.RecoveryState,
                    RestartCount = current.RestartCount,
                    LastHealthCheck = current.LastHealthCheck,
                    LastRestart = current.LastRestart,
                    DateCreated = current.DateCreated,
                    DateModified = now,
                };
                await _repository.UpsertAsync(updated, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                var created = new VirtualMachine
                {
                    Id = configEntry.Id,
                    ExternalId = configEntry.ExternalId,
                    Name = configEntry.Name,
                    ProviderId = configEntry.ProviderId,
                    Enabled = configEntry.Enabled,
                    HealthProfileId = configEntry.HealthProfileId,
                    RecoveryProfileId = configEntry.RecoveryProfileId,
                    Tags = configEntry.Tags,
                    DateCreated = now,
                    DateModified = now,
                };
                await _repository.UpsertAsync(created, cancellationToken).ConfigureAwait(false);
                _logger.LogInformation("Registered new monitored virtual machine {Name}", configEntry.Name);
            }
        }

        foreach (var removedId in existing.Keys.Except(configuredIds))
        {
            await _repository.DeleteAsync(removedId, cancellationToken).ConfigureAwait(false);
            _logger.LogInformation("Removed virtual machine {VirtualMachineId} no longer present in configuration", removedId);
        }
    }
}

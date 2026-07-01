using Dapper;
using FireTower.Core.Interfaces;
using FireTower.Core.Models;
using FireTower.Data.Entities;
using FireTower.Data.Services;
using FireTower.Shared.Enums;

namespace FireTower.Data.Repositories;

/// <summary>
/// SQLite-backed implementation of <see cref="IVirtualMachineRepository"/>.
/// </summary>
public sealed class VirtualMachineRepository : IVirtualMachineRepository
{
    private readonly ISqliteConnectionFactory _connectionFactory;

    public VirtualMachineRepository(ISqliteConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<VirtualMachine>> GetAllAsync(CancellationToken cancellationToken)
    {
        using var connection = _connectionFactory.CreateConnection();
        var rows = await connection.QueryAsync<VirtualMachineEntity>(
            "SELECT * FROM VirtualMachines ORDER BY Name;").ConfigureAwait(false);
        return rows.Select(ToModel).ToList();
    }

    public async Task<VirtualMachine?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        using var connection = _connectionFactory.CreateConnection();
        var row = await connection.QuerySingleOrDefaultAsync<VirtualMachineEntity>(
            "SELECT * FROM VirtualMachines WHERE Id = @Id;", new { Id = id.ToString() }).ConfigureAwait(false);
        return row is null ? null : ToModel(row);
    }

    public async Task UpsertAsync(VirtualMachine virtualMachine, CancellationToken cancellationToken)
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync("""
            INSERT INTO VirtualMachines
                (Id, ExternalId, Name, ProviderId, Enabled, HealthProfileId, RecoveryProfileId, Tags,
                 PowerState, Health, RecoveryState, RestartCount, LastHealthCheck, LastRestart, DateCreated, DateModified)
            VALUES
                (@Id, @ExternalId, @Name, @ProviderId, @Enabled, @HealthProfileId, @RecoveryProfileId, @Tags,
                 @PowerState, @Health, @RecoveryState, @RestartCount, @LastHealthCheck, @LastRestart, @DateCreated, @DateModified)
            ON CONFLICT(Id) DO UPDATE SET
                ExternalId = @ExternalId, Name = @Name, ProviderId = @ProviderId, Enabled = @Enabled,
                HealthProfileId = @HealthProfileId, RecoveryProfileId = @RecoveryProfileId, Tags = @Tags,
                PowerState = @PowerState, Health = @Health, RecoveryState = @RecoveryState,
                RestartCount = @RestartCount, LastHealthCheck = @LastHealthCheck, LastRestart = @LastRestart,
                DateModified = @DateModified;
            """, ToEntity(virtualMachine)).ConfigureAwait(false);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(
            "DELETE FROM VirtualMachines WHERE Id = @Id;", new { Id = id.ToString() }).ConfigureAwait(false);
    }

    private static VirtualMachineEntity ToEntity(VirtualMachine vm) => new()
    {
        Id = vm.Id.ToString(),
        ExternalId = vm.ExternalId,
        Name = vm.Name,
        ProviderId = vm.ProviderId,
        Enabled = vm.Enabled,
        HealthProfileId = vm.HealthProfileId,
        RecoveryProfileId = vm.RecoveryProfileId,
        Tags = string.Join(',', vm.Tags),
        PowerState = vm.PowerState.ToString(),
        Health = vm.Health.ToString(),
        RecoveryState = vm.RecoveryState.ToString(),
        RestartCount = vm.RestartCount,
        LastHealthCheck = vm.LastHealthCheck,
        LastRestart = vm.LastRestart,
        DateCreated = vm.DateCreated,
        DateModified = vm.DateModified,
    };

    private static VirtualMachine ToModel(VirtualMachineEntity entity) => new()
    {
        Id = Guid.Parse(entity.Id),
        ExternalId = entity.ExternalId,
        Name = entity.Name,
        ProviderId = entity.ProviderId,
        HealthProfileId = entity.HealthProfileId,
        RecoveryProfileId = entity.RecoveryProfileId,
        Enabled = entity.Enabled,
        Tags = entity.Tags.Length == 0 ? Array.Empty<string>() : entity.Tags.Split(','),
        PowerState = Enum.Parse<VmPowerState>(entity.PowerState),
        Health = Enum.Parse<HealthState>(entity.Health),
        RecoveryState = Enum.Parse<RecoveryState>(entity.RecoveryState),
        RestartCount = entity.RestartCount,
        LastHealthCheck = entity.LastHealthCheck,
        LastRestart = entity.LastRestart,
        DateCreated = entity.DateCreated,
        DateModified = entity.DateModified,
    };
}

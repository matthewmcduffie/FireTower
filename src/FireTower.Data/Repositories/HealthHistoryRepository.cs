using System.Text.Json;
using Dapper;
using FireTower.Core.Interfaces;
using FireTower.Core.Models;
using FireTower.Data.Entities;
using FireTower.Data.Services;
using FireTower.Shared.Enums;

namespace FireTower.Data.Repositories;

/// <summary>
/// SQLite-backed implementation of <see cref="IHealthHistoryRepository"/>.
/// </summary>
public sealed class HealthHistoryRepository : IHealthHistoryRepository
{
    private readonly ISqliteConnectionFactory _connectionFactory;

    public HealthHistoryRepository(ISqliteConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task RecordAsync(HealthEvaluation evaluation, CancellationToken cancellationToken)
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync("""
            INSERT INTO HealthHistory (VirtualMachineId, PreviousState, NewState, CheckResultsJson, Timestamp)
            VALUES (@VirtualMachineId, @PreviousState, @NewState, @CheckResultsJson, @Timestamp);
            """, new
        {
            VirtualMachineId = evaluation.VirtualMachineId.ToString(),
            PreviousState = evaluation.PreviousState.ToString(),
            NewState = evaluation.NewState.ToString(),
            CheckResultsJson = JsonSerializer.Serialize(evaluation.CheckResults),
            evaluation.Timestamp,
        }).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<HealthEvaluation>> GetRecentAsync(Guid virtualMachineId, int take, CancellationToken cancellationToken)
    {
        using var connection = _connectionFactory.CreateConnection();
        var rows = await connection.QueryAsync<HealthHistoryEntity>("""
            SELECT * FROM HealthHistory
            WHERE VirtualMachineId = @VirtualMachineId
            ORDER BY Timestamp DESC
            LIMIT @Take;
            """, new { VirtualMachineId = virtualMachineId.ToString(), Take = take }).ConfigureAwait(false);

        return rows.Select(ToModel).ToList();
    }

    public async Task PurgeOlderThanAsync(DateTimeOffset cutoff, CancellationToken cancellationToken)
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(
            "DELETE FROM HealthHistory WHERE Timestamp < @Cutoff;", new { Cutoff = cutoff }).ConfigureAwait(false);
    }

    private static HealthEvaluation ToModel(HealthHistoryEntity entity) => new()
    {
        VirtualMachineId = Guid.Parse(entity.VirtualMachineId),
        PreviousState = Enum.Parse<HealthState>(entity.PreviousState),
        NewState = Enum.Parse<HealthState>(entity.NewState),
        CheckResults = JsonSerializer.Deserialize<List<HealthCheckResult>>(entity.CheckResultsJson) ?? new(),
        Timestamp = entity.Timestamp,
    };
}

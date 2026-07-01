using Dapper;
using FireTower.Core.Interfaces;
using FireTower.Core.Models;
using FireTower.Data.Entities;
using FireTower.Data.Services;
using FireTower.Shared.Enums;

namespace FireTower.Data.Repositories;

/// <summary>
/// SQLite-backed implementation of <see cref="IRestartHistoryRepository"/>. Rows are never
/// modified after insertion, per database.md.
/// </summary>
public sealed class RestartHistoryRepository : IRestartHistoryRepository
{
    private readonly ISqliteConnectionFactory _connectionFactory;

    public RestartHistoryRepository(ISqliteConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task RecordAsync(RecoveryResult result, CancellationToken cancellationToken)
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync("""
            INSERT INTO RestartHistory
                (VirtualMachineId, Action, Success, FailureCategory, FailureReason, DurationMs, Timestamp, CorrelationId)
            VALUES
                (@VirtualMachineId, @Action, @Success, @FailureCategory, @FailureReason, @DurationMs, @Timestamp, @CorrelationId);
            """, new
        {
            VirtualMachineId = result.VirtualMachineId.ToString(),
            Action = result.Action.ToString(),
            result.Success,
            FailureCategory = result.FailureCategory.ToString(),
            result.FailureReason,
            DurationMs = (long)result.Duration.TotalMilliseconds,
            result.Timestamp,
            result.CorrelationId,
        }).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<RecoveryResult>> GetRecentAsync(Guid virtualMachineId, int take, CancellationToken cancellationToken)
    {
        using var connection = _connectionFactory.CreateConnection();
        var rows = await connection.QueryAsync<RestartHistoryEntity>("""
            SELECT * FROM RestartHistory
            WHERE VirtualMachineId = @VirtualMachineId
            ORDER BY Timestamp DESC
            LIMIT @Take;
            """, new { VirtualMachineId = virtualMachineId.ToString(), Take = take }).ConfigureAwait(false);

        return rows.Select(ToModel).ToList();
    }

    public async Task<int> CountWithinWindowAsync(Guid virtualMachineId, DateTimeOffset since, CancellationToken cancellationToken)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<int>("""
            SELECT COUNT(*) FROM RestartHistory
            WHERE VirtualMachineId = @VirtualMachineId AND Timestamp >= @Since;
            """, new { VirtualMachineId = virtualMachineId.ToString(), Since = since }).ConfigureAwait(false);
    }

    public async Task PurgeOlderThanAsync(DateTimeOffset cutoff, CancellationToken cancellationToken)
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(
            "DELETE FROM RestartHistory WHERE Timestamp < @Cutoff;", new { Cutoff = cutoff }).ConfigureAwait(false);
    }

    private static RecoveryResult ToModel(RestartHistoryEntity entity) => new()
    {
        VirtualMachineId = Guid.Parse(entity.VirtualMachineId),
        Action = Enum.Parse<RecoveryAction>(entity.Action),
        Success = entity.Success,
        FailureCategory = Enum.Parse<RecoveryFailureCategory>(entity.FailureCategory),
        FailureReason = entity.FailureReason,
        Duration = TimeSpan.FromMilliseconds(entity.DurationMs),
        Timestamp = entity.Timestamp,
        CorrelationId = entity.CorrelationId,
    };
}

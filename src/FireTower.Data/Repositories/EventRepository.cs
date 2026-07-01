using Dapper;
using FireTower.Core.Interfaces;
using FireTower.Core.Models;
using FireTower.Data.Entities;
using FireTower.Data.Services;

namespace FireTower.Data.Repositories;

/// <summary>
/// SQLite-backed implementation of <see cref="IEventRepository"/>.
/// </summary>
public sealed class EventRepository : IEventRepository
{
    private readonly ISqliteConnectionFactory _connectionFactory;

    public EventRepository(ISqliteConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task RecordAsync(OperationalEvent operationalEvent, CancellationToken cancellationToken)
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync("""
            INSERT INTO Events (Timestamp, Category, Message, VirtualMachineId, CorrelationId)
            VALUES (@Timestamp, @Category, @Message, @VirtualMachineId, @CorrelationId);
            """, new
        {
            operationalEvent.Timestamp,
            operationalEvent.Category,
            operationalEvent.Message,
            VirtualMachineId = operationalEvent.VirtualMachineId?.ToString(),
            operationalEvent.CorrelationId,
        }).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<OperationalEvent>> GetRecentAsync(int skip, int take, CancellationToken cancellationToken)
    {
        using var connection = _connectionFactory.CreateConnection();
        var rows = await connection.QueryAsync<EventEntity>("""
            SELECT * FROM Events
            ORDER BY Timestamp DESC
            LIMIT @Take OFFSET @Skip;
            """, new { Skip = skip, Take = take }).ConfigureAwait(false);

        return rows.Select(ToModel).ToList();
    }

    public async Task PurgeOlderThanAsync(DateTimeOffset cutoff, CancellationToken cancellationToken)
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(
            "DELETE FROM Events WHERE Timestamp < @Cutoff;", new { Cutoff = cutoff }).ConfigureAwait(false);
    }

    private static OperationalEvent ToModel(EventEntity entity) => new()
    {
        Timestamp = entity.Timestamp,
        Category = entity.Category,
        Message = entity.Message,
        VirtualMachineId = entity.VirtualMachineId is null ? null : Guid.Parse(entity.VirtualMachineId),
        CorrelationId = entity.CorrelationId,
    };
}

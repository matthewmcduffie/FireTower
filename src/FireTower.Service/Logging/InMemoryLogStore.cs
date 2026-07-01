using System.Collections.Concurrent;
using FireTower.Shared.DTOs;

namespace FireTower.Service.Logging;

/// <summary>
/// A bounded, thread-safe ring buffer of recent log entries, queried directly by the IPC
/// <c>GetLogs</c> handler. Full history always remains on disk via the rolling file sinks
/// configured in <see cref="SerilogConfigurator"/>; this buffer exists only so the tray
/// application's Logs page does not need to parse log files over IPC.
/// </summary>
public sealed class InMemoryLogStore
{
    private const int Capacity = 5000;
    private readonly ConcurrentQueue<LogEntryDto> _entries = new();

    public void Add(LogEntryDto entry)
    {
        _entries.Enqueue(entry);
        while (_entries.Count > Capacity && _entries.TryDequeue(out _))
        {
        }
    }

    public IReadOnlyList<LogEntryDto> Query(LogQueryDto query)
    {
        IEnumerable<LogEntryDto> results = _entries;

        if (!string.IsNullOrWhiteSpace(query.Component))
        {
            results = results.Where(e => e.Component.Contains(query.Component, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(query.SearchText))
        {
            results = results.Where(e => e.Message.Contains(query.SearchText, StringComparison.OrdinalIgnoreCase));
        }

        if (query.Since is { } since)
        {
            results = results.Where(e => e.Timestamp >= since);
        }

        if (query.Until is { } until)
        {
            results = results.Where(e => e.Timestamp <= until);
        }

        return results
            .OrderByDescending(e => e.Timestamp)
            .Skip(query.Skip)
            .Take(query.Take)
            .ToList();
    }
}

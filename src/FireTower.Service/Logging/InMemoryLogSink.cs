using FireTower.Shared.DTOs;
using Serilog.Core;
using Serilog.Events;

namespace FireTower.Service.Logging;

/// <summary>
/// Serilog sink that mirrors every log event into <see cref="InMemoryLogStore"/>.
/// </summary>
public sealed class InMemoryLogSink : ILogEventSink
{
    private readonly InMemoryLogStore _store;

    public InMemoryLogSink(InMemoryLogStore store)
    {
        _store = store;
    }

    public void Emit(LogEvent logEvent)
    {
        logEvent.Properties.TryGetValue("SourceContext", out var sourceContextValue);
        logEvent.Properties.TryGetValue("VirtualMachine", out var vmValue);
        logEvent.Properties.TryGetValue("CorrelationId", out var correlationValue);

        _store.Add(new LogEntryDto
        {
            Timestamp = logEvent.Timestamp,
            Level = logEvent.Level.ToString(),
            Component = sourceContextValue?.ToString().Trim('"') ?? "FireTower",
            Message = logEvent.RenderMessage(),
            VirtualMachine = vmValue?.ToString().Trim('"'),
            CorrelationId = correlationValue?.ToString().Trim('"'),
            Exception = logEvent.Exception?.ToString(),
        });
    }
}

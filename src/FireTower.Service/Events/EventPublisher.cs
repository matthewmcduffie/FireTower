using FireTower.Core.Interfaces;
using FireTower.Core.Models;
using Microsoft.Extensions.Logging;

namespace FireTower.Service.Events;

/// <summary>
/// Default <see cref="IEventPublisher"/> implementation. Persists operational events to the
/// database and raises CLR events so the IPC host (FireTower.Service.Ipc) can forward them
/// to connected clients without the rest of the application knowing IPC exists.
/// </summary>
public sealed class EventPublisher : IEventPublisher
{
    private readonly IEventRepository _eventRepository;
    private readonly ILogger<EventPublisher> _logger;

    public EventPublisher(IEventRepository eventRepository, ILogger<EventPublisher> logger)
    {
        _eventRepository = eventRepository;
        _logger = logger;
    }

    public event EventHandler<VirtualMachine>? VmStatusChanged;
    public event EventHandler? ConfigurationReloaded;
    public event EventHandler<ProviderRegistration>? ProviderStatusChanged;

    public void PublishVmStatusChanged(VirtualMachine virtualMachine)
    {
        VmStatusChanged?.Invoke(this, virtualMachine);
    }

    public void PublishConfigurationReloaded()
    {
        ConfigurationReloaded?.Invoke(this, EventArgs.Empty);
    }

    public void PublishProviderStatusChanged(ProviderRegistration provider)
    {
        ProviderStatusChanged?.Invoke(this, provider);
    }

    public void PublishOperationalEvent(string category, string message, Guid? virtualMachineId, string? correlationId)
    {
        _ = RecordAsync(category, message, virtualMachineId, correlationId);
    }

    private async Task RecordAsync(string category, string message, Guid? virtualMachineId, string? correlationId)
    {
        try
        {
            await _eventRepository.RecordAsync(new OperationalEvent
            {
                Timestamp = DateTimeOffset.UtcNow,
                Category = category,
                Message = message,
                VirtualMachineId = virtualMachineId,
                CorrelationId = correlationId,
            }, CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record operational event in category {Category}", category);
        }
    }
}

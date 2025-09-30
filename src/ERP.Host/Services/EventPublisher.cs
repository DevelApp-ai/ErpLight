using ERP.SharedKernel.Events;

namespace ERP.Host.Services;

/// <summary>
/// Implementation of IEventPublisher that handles domain event publishing and distribution.
/// </summary>
public class EventPublisher : IEventPublisher
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<EventPublisher> _logger;

    public EventPublisher(IServiceProvider serviceProvider, ILogger<EventPublisher> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <summary>
    /// Publishes a domain event to all registered handlers.
    /// </summary>
    /// <param name="domainEvent">The event to publish.</param>
    /// <returns>A task representing the publishing operation.</returns>
    public async Task PublishAsync(DomainEvent domainEvent)
    {
        _logger.LogDebug("Publishing event: {EventType} with ID {EventId}", domainEvent.EventType, domainEvent.EventId);

        var eventType = domainEvent.GetType();
        var handlerType = typeof(IEventHandler<>).MakeGenericType(eventType);
        
        using var scope = _serviceProvider.CreateScope();
        var handlers = scope.ServiceProvider.GetServices(handlerType);
        
        var handlerTasks = new List<Task>();

        foreach (var handler in handlers)
        {
            try
            {
                // Use reflection to call HandleAsync method
                var handleMethod = handlerType.GetMethod("HandleAsync");
                if (handleMethod != null)
                {
                    var result = handleMethod.Invoke(handler, new object[] { domainEvent });
                    if (result is Task task)
                    {
                        handlerTasks.Add(task);
                    }
                }
            }
            catch (Exception ex)
            {
                if (handler != null)
                {
                    _logger.LogError(ex, "Error invoking event handler {HandlerType} for event {EventType}", 
                        handler.GetType().Name, domainEvent.EventType);
                }
                else
                {
                    _logger.LogError(ex, "Error invoking event handler for event {EventType} (handler was null)", domainEvent.EventType);
                }
            }
        }

        if (handlerTasks.Any())
        {
            await Task.WhenAll(handlerTasks);
            _logger.LogDebug("Successfully published event {EventType} to {HandlerCount} handlers", 
                domainEvent.EventType, handlerTasks.Count);
        }
        else
        {
            _logger.LogDebug("No handlers found for event {EventType}", domainEvent.EventType);
        }
    }
}
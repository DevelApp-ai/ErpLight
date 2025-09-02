namespace ERP.SharedKernel.Events;

/// <summary>
/// Base class for all domain events in the system.
/// Events enable loose coupling between plugins by allowing asynchronous communication.
/// </summary>
public abstract class DomainEvent
{
    /// <summary>
    /// Gets the unique identifier for this event instance.
    /// </summary>
    public Guid EventId { get; } = Guid.NewGuid();

    /// <summary>
    /// Gets the timestamp when this event was created.
    /// </summary>
    public DateTime OccurredAt { get; } = DateTime.UtcNow;

    /// <summary>
    /// Gets the name/type of this event.
    /// </summary>
    public string EventType => GetType().Name;
}

/// <summary>
/// Interface for handling domain events.
/// Plugins can implement this interface to react to events from other plugins.
/// </summary>
/// <typeparam name="TEvent">The type of event to handle.</typeparam>
public interface IEventHandler<in TEvent> where TEvent : DomainEvent
{
    /// <summary>
    /// Handles the specified domain event.
    /// </summary>
    /// <param name="domainEvent">The event to handle.</param>
    /// <returns>A task representing the handling operation.</returns>
    Task HandleAsync(TEvent domainEvent);
}

/// <summary>
/// Interface for publishing domain events.
/// The host application provides an implementation of this interface.
/// </summary>
public interface IEventPublisher
{
    /// <summary>
    /// Publishes a domain event to all registered handlers.
    /// </summary>
    /// <param name="domainEvent">The event to publish.</param>
    /// <returns>A task representing the publishing operation.</returns>
    Task PublishAsync(DomainEvent domainEvent);
}
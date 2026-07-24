#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Collections.Concurrent;

namespace GrpcWebBridge.Events;

/// <summary>
/// Event bus for publish-subscribe pattern.
/// Enables loose coupling between components through event-driven architecture.
/// Supports synchronous and asynchronous event handling.
/// </summary>
public sealed class EventBus : IDisposable
{
    private readonly ConcurrentDictionary<string, List<Delegate>> _subscribers;
    private readonly ILogger<EventBus> _logger;
    private readonly ConcurrentBag<EventRecord> _eventHistory;
    private readonly int _maxHistorySize;
    private int _disposed;

    /// <summary>
    /// Gets a value indicating whether this instance has been disposed.
    /// </summary>
    public bool IsDisposed => _disposed == 1;

    public EventBus(ILogger<EventBus> logger, int maxHistorySize = 1000)
    {
        ArgumentNullException.ThrowIfNull(logger);

        _subscribers = new ConcurrentDictionary<string, List<Delegate>>();
        _logger = logger;
        _eventHistory = new ConcurrentBag<EventRecord>();
        _maxHistorySize = maxHistorySize;
    }

    /// <summary>
    /// Subscribes to an event with a synchronous handler.
    /// </summary>
    /// <typeparam name="TEvent">The event type to subscribe to.</typeparam>
    /// <param name="handler">The synchronous handler to invoke when the event is published.</param>
    /// <exception cref="ArgumentNullException"><paramref name="handler"/> is <see langword="null"/>.</exception>
    /// <exception cref="ObjectDisposedException">The event bus has been disposed.</exception>
    public void Subscribe<TEvent>(Action<TEvent> handler) where TEvent : EventBase
    {
        ArgumentNullException.ThrowIfNull(handler);
        ObjectDisposedException.ThrowIf(IsDisposed, this);

        var eventName = typeof(TEvent).Name;
        var handlers = _subscribers.GetOrAdd(eventName, _ => new List<Delegate>());

        lock (handlers)
        {
            handlers.Add(handler);
        }

        _logger.LogDebug("Subscribed to event: EventType={EventType}", eventName);
    }

    /// <summary>
    /// Subscribes to an event with an asynchronous handler.
    /// </summary>
    /// <typeparam name="TEvent">The event type to subscribe to.</typeparam>
    /// <param name="handler">The asynchronous handler to invoke when the event is published.</param>
    /// <exception cref="ArgumentNullException"><paramref name="handler"/> is <see langword="null"/>.</exception>
    /// <exception cref="ObjectDisposedException">The event bus has been disposed.</exception>
    public void Subscribe<TEvent>(Func<TEvent, Task> handler) where TEvent : EventBase
    {
        ArgumentNullException.ThrowIfNull(handler);
        ObjectDisposedException.ThrowIf(IsDisposed, this);

        var eventName = typeof(TEvent).Name;
        var handlers = _subscribers.GetOrAdd(eventName, _ => new List<Delegate>());

        lock (handlers)
        {
            handlers.Add(handler);
        }

        _logger.LogDebug("Subscribed to async event: EventType={EventType}", eventName);
    }

    /// <summary>
    /// Unsubscribes from an event.
    /// </summary>
    /// <typeparam name="TEvent">The event type to unsubscribe from.</typeparam>
    /// <param name="handler">The handler to remove.</param>
    /// <returns><see langword="true"/> if the handler was found and removed; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="handler"/> is <see langword="null"/>.</exception>
    /// <exception cref="ObjectDisposedException">The event bus has been disposed.</exception>
    public bool Unsubscribe<TEvent>(Delegate handler) where TEvent : EventBase
    {
        ArgumentNullException.ThrowIfNull(handler);
        ObjectDisposedException.ThrowIf(IsDisposed, this);

        var eventName = typeof(TEvent).Name;
        if (!_subscribers.TryGetValue(eventName, out var handlers))
        {
            return false;
        }

        lock (handlers)
        {
            var removed = handlers.Remove(handler);
            if (removed)
            {
                _logger.LogDebug("Unsubscribed from event: EventType={EventType}", eventName);
            }
            return removed;
        }
    }

    /// <summary>
    /// Publishes an event synchronously.
    /// All subscribers are notified immediately.
    /// </summary>
    /// <typeparam name="TEvent">The event type to publish.</typeparam>
    /// <param name="event">The event to publish.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="event"/> is <see langword="null"/>.</exception>
    /// <exception cref="ObjectDisposedException">The event bus has been disposed.</exception>
    public async Task PublishAsync<TEvent>(TEvent @event) where TEvent : EventBase
    {
        ArgumentNullException.ThrowIfNull(@event);
        ObjectDisposedException.ThrowIf(IsDisposed, this);

        var eventName = typeof(TEvent).Name;
        RecordEvent(@event);

        if (!_subscribers.TryGetValue(eventName, out var handlers) || handlers.Count == 0)
        {
            _logger.LogDebug("Published event with no subscribers: EventType={EventType}", eventName);
            return;
        }

        // Take a snapshot of handlers to avoid issues with concurrent modifications during iteration
        List<Delegate>? handlerSnapshot = null;
        lock (handlers)
        {
            handlerSnapshot = new List<Delegate>(handlers);
        }

        var exceptions = new List<Exception>();

        foreach (var handler in handlerSnapshot)
        {
            try
            {
                switch (handler)
                {
                    case Action<TEvent> syncHandler:
                        syncHandler(@event);
                        break;
                    case Func<TEvent, Task> asyncHandler:
                        await asyncHandler(@event).ConfigureAwait(false);
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing event handler: EventType={EventType}", eventName);
                exceptions.Add(ex);
            }
        }

        // If any exceptions occurred, aggregate them into a single exception
        if (exceptions.Count > 0)
        {
            throw new EventBusException($"One or more event handlers failed for event type {eventName}", new AggregateException(exceptions));
        }

        _logger.LogInformation("Published event: EventType={EventType}, SubscriberCount={Count}", eventName, handlers.Count);
    }

    /// <summary>
    /// Gets the number of subscribers for an event type.
    /// </summary>
    /// <typeparam name="TEvent">The event type to check.</typeparam>
    /// <returns>The number of subscribers for the specified event type.</returns>
    public int GetSubscriberCount<TEvent>() where TEvent : EventBase
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);

        var eventName = typeof(TEvent).Name;
        return _subscribers.TryGetValue(eventName, out var handlers) ? handlers.Count : 0;
    }

    /// <summary>
    /// Gets event history for auditing and debugging.
    /// </summary>
    /// <param name="eventType">Optional event type filter.</param>
    /// <returns>A list of event records.</returns>
    /// <exception cref="ObjectDisposedException">The event bus has been disposed.</exception>
    public List<EventRecord> GetEventHistory(string? eventType = null)
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);

        var history = _eventHistory.ToList();

        if (!string.IsNullOrEmpty(eventType))
        {
            history = history.Where(h => h.EventType == eventType).ToList();
        }

        return history.OrderByDescending(h => h.PublishedAt).ToList();
    }

    /// <summary>
    /// Clears all subscribers.
    /// Useful for testing and cleanup.
    /// </summary>
    /// <exception cref="ObjectDisposedException">The event bus has been disposed.</exception>
    public void ClearSubscribers()
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);

        _subscribers.Clear();
        _logger.LogInformation("All event subscribers cleared");
    }

    /// <summary>
    /// Records published event for history.
    /// </summary>
    private void RecordEvent<TEvent>(TEvent @event) where TEvent : EventBase
    {
        var record = new EventRecord
        {
            EventType = typeof(TEvent).Name,
            EventId = @event.EventId,
            PublishedAt = DateTime.UtcNow,
            Data = @event
        };

        _eventHistory.Add(record);

        // Trim history if it exceeds max size
        if (_eventHistory.Count > _maxHistorySize)
        {
            var oldestEvents = _eventHistory
                .OrderBy(e => e.PublishedAt)
                .Take(_eventHistory.Count - _maxHistorySize)
                .ToList();

            foreach (var oldEvent in oldestEvents)
            {
                _eventHistory.TryTake(out _);
            }
        }
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 0)
        {
            _subscribers.Clear();
            _logger.LogInformation("EventBus disposed");
        }
    }
}

/// <summary>
/// Base class for all events.
/// </summary>
public abstract class EventBase
{
    public string EventId { get; } = Guid.NewGuid().ToString();
    public DateTime CreatedAt { get; } = DateTime.UtcNow;
    public string? Source { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
}

/// <summary>
/// Event record for history tracking.
/// </summary>
public sealed class EventRecord
{
    public string EventType { get; set; } = string.Empty;
    public string EventId { get; set; } = string.Empty;
    public DateTime PublishedAt { get; set; }
    public object? Data { get; set; }
}

/// <summary>
/// Exception thrown when event bus operations fail.
/// </summary>
public sealed class EventBusException : Exception
{
    public EventBusException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}

// Concrete event types for the bridge

/// <summary>
/// Fired when a service is registered.
/// </summary>
public class ServiceRegisteredEvent : EventBase
{
    public string ServiceId { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
}

/// <summary>
/// Fired when a service is unregistered.
/// </summary>
public class ServiceUnregisteredEvent : EventBase
{
    public string ServiceId { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
}

/// <summary>
/// Fired when a method is invoked.
/// </summary>
public class MethodInvokedEvent : EventBase
{
    public string ServiceId { get; set; } = string.Empty;
    public string MethodName { get; set; } = string.Empty;
    public long DurationMs { get; set; }
    public bool Success { get; set; }
}

/// <summary>
/// Fired when a stream starts.
/// </summary>
public class StreamStartedEvent : EventBase
{
    public string StreamId { get; set; } = string.Empty;
    public string ServiceId { get; set; } = string.Empty;
    public string MethodName { get; set; } = string.Empty;
}

/// <summary>
/// Fired when a stream ends.
/// </summary>
public class StreamEndedEvent : EventBase
{
    public string StreamId { get; set; } = string.Empty;
    public long MessageCount { get; set; }
    public long DurationMs { get; set; }
}

/// <summary>
/// Fired when authentication fails.
/// </summary>
public class AuthenticationFailedEvent : EventBase
{
    public string? UserId { get; set; }
    public string FailureReason { get; set; } = string.Empty;
    public string? ClientIp { get; set; }
}
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
public class EventBus : IDisposable
{
    private readonly ConcurrentDictionary<string, List<Delegate>> _subscribers;
    private readonly ILogger<EventBus> _logger;
    private readonly ConcurrentBag<EventRecord> _eventHistory;
    private readonly int _maxHistorySize;

    public EventBus(ILogger<EventBus> logger, int maxHistorySize = 1000)
    {
        _subscribers = new ConcurrentDictionary<string, List<Delegate>>();
        _logger = logger;
        _eventHistory = new ConcurrentBag<EventRecord>();
        _maxHistorySize = maxHistorySize;
    }

    /// <summary>
    /// Subscribes to an event with a synchronous handler.
    /// </summary>
    public void Subscribe<TEvent>(Action<TEvent> handler) where TEvent : EventBase
    {
        if (handler is null)
            throw new ArgumentNullException(nameof(handler));

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
    public void Subscribe<TEvent>(Func<TEvent, Task> handler) where TEvent : EventBase
    {
        if (handler is null)
            throw new ArgumentNullException(nameof(handler));

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
    public bool Unsubscribe<TEvent>(Delegate handler) where TEvent : EventBase
    {
        if (handler is null)
            return false;

        var eventName = typeof(TEvent).Name;
        if (!_subscribers.TryGetValue(eventName, out var handlers))
            return false;

        lock (handlers)
        {
            var removed = handlers.Remove(handler);
            if (removed)
                _logger.LogDebug("Unsubscribed from event: EventType={EventType}", eventName);
            return removed;
        }
    }

    /// <summary>
    /// Publishes an event synchronously.
    /// All subscribers are notified immediately.
    /// </summary>
    public async Task PublishAsync<TEvent>(TEvent @event) where TEvent : EventBase
    {
        if (@event is null)
            throw new ArgumentNullException(nameof(@event));

        var eventName = typeof(TEvent).Name;
        RecordEvent(@event);

        if (!_subscribers.TryGetValue(eventName, out var handlers) || handlers.Count == 0)
        {
            _logger.LogDebug("Published event with no subscribers: EventType={EventType}", eventName);
            return;
        }

        var tasks = new List<Task>();

        lock (handlers)
        {
            foreach (var handler in handlers)
            {
                try
                {
                    if (handler is Action<TEvent> syncHandler)
                    {
                        syncHandler(@event);
                    }
                    else if (handler is Func<TEvent, Task> asyncHandler)
                    {
                        tasks.Add(asyncHandler(@event));
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error executing event handler: EventType={EventType}", eventName);
                }
            }
        }

        if (tasks.Count > 0)
        {
            await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        _logger.LogInformation("Published event: EventType={EventType}, SubscriberCount={Count}", eventName, handlers.Count);
    }

    /// <summary>
    /// Gets the number of subscribers for an event type.
    /// </summary>
    public int GetSubscriberCount<TEvent>() where TEvent : EventBase
    {
        var eventName = typeof(TEvent).Name;
        return _subscribers.TryGetValue(eventName, out var handlers) ? handlers.Count : 0;
    }

    /// <summary>
    /// Gets event history for auditing and debugging.
    /// </summary>
    public List<EventRecord> GetEventHistory(string? eventType = null)
    {
        var history = _eventHistory.ToList();

        if (!string.IsNullOrEmpty(eventType))
            history = history.Where(h => h.EventType == eventType).ToList();

        return history.OrderByDescending(h => h.PublishedAt).ToList();
    }

    /// <summary>
    /// Clears all subscribers.
    /// Useful for testing and cleanup.
    /// </summary>
    public void ClearSubscribers()
    {
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
        _subscribers?.Clear();
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

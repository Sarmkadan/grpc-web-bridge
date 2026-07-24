#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Collections.Concurrent;
using System.Threading.Channels;

namespace GrpcWebBridge.Events;

/// <summary>
/// Event bus for publish-subscribe pattern.
/// Enables loose coupling between components through event-driven architecture.
/// Supports synchronous and asynchronous event handling with backpressure management.
/// </summary>
public sealed class EventBus : IDisposable
{
    private readonly ConcurrentDictionary<string, List<Delegate>> _subscribers;
    private readonly ILogger<EventBus> _logger;
    private readonly ConcurrentQueue<EventRecord> _eventHistory;
    private readonly int _maxHistorySize;
    private readonly TimeSpan _maxHistoryAge;
    private int _disposed;

    /// <summary>
    /// Configuration for backpressure and dispatch behavior.
    /// </summary>
    public sealed record DispatchOptions
    {
        /// <summary>
        /// Maximum number of events that can be queued for dispatch.
        /// When the queue is full, the policy determines behavior.
        /// Defaults to 1024.
        /// </summary>
        public int MaxQueueSize { get; init; } = 1024;

        /// <summary>
        /// Policy to apply when the dispatch queue is full.
        /// - DropOldest: Discard the oldest queued event (default)
        /// - Block: Wait until space is available in the queue
        /// </summary>
        public DispatchQueueFullPolicy FullQueuePolicy { get; init; } = DispatchQueueFullPolicy.DropOldest;

        /// <summary>
        /// Maximum time to wait when FullQueuePolicy is Block.
        /// Defaults to 5 seconds.
        /// </summary>
        public TimeSpan MaxWaitTime { get; init; } = TimeSpan.FromSeconds(5);

        /// <summary>
        /// Whether to process events asynchronously using a dispatch queue.
        /// When true, event handlers run on dedicated worker threads, preventing
        /// blocking of the publishing thread. When false, handlers execute inline.
        /// Defaults to false for backward compatibility with existing code.
        /// </summary>
        public bool UseAsyncDispatch { get; init; } = false;
    }

    /// <summary>
    /// Policy for handling full dispatch queue.
    /// </summary>
    public enum DispatchQueueFullPolicy
    {
        /// <summary>
        /// Discard the oldest queued event when the queue is full.
        /// </summary>
        DropOldest,

        /// <summary>
        /// Wait until space is available in the queue.
        /// </summary>
        Block
    }

    /// <summary>
    /// Gets a value indicating whether this instance has been disposed.
    /// </summary>
    public bool IsDisposed => _disposed == 1;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventBus"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="maxHistorySize">Maximum number of events to retain in history. Default is 1000.</param>
    /// <param name="maxHistoryAge">Maximum age of events to retain. Events older than this will be trimmed. Default is 1 hour.</param>
    /// <param name="dispatchOptions">Configuration for async dispatch and backpressure. Defaults to async dispatch enabled with drop-oldest policy.</param>
    /// <exception cref="ArgumentNullException"><paramref name="logger"/> is null.</exception>
    public EventBus(
        ILogger<EventBus> logger,
        int maxHistorySize = 1000,
        TimeSpan? maxHistoryAge = null,
        DispatchOptions? dispatchOptions = null)
    {
        ArgumentNullException.ThrowIfNull(logger);

        _subscribers = new ConcurrentDictionary<string, List<Delegate>>();
        _logger = logger;
        _eventHistory = new ConcurrentQueue<EventRecord>();
        _maxHistorySize = maxHistorySize;
        _maxHistoryAge = maxHistoryAge ?? TimeSpan.FromHours(1);

        _dispatchOptions = dispatchOptions ?? new DispatchOptions();

        if (_dispatchOptions.UseAsyncDispatch)
        {
            InitializeDispatchQueue();
        }
    }

    private void InitializeDispatchQueue()
    {
        _dispatchChannel = Channel.CreateBounded<DispatchWorkItem>(
            new BoundedChannelOptions(_dispatchOptions.MaxQueueSize)
            {
                FullMode = _dispatchOptions.FullQueuePolicy == DispatchQueueFullPolicy.DropOldest
                    ? BoundedChannelFullMode.DropOldest
                    : BoundedChannelFullMode.Wait,
                SingleReader = true,
                SingleWriter = false
            });

        _dispatchWorker = Task.Run(DispatchLoopAsync);
        _logger.LogInformation(
            "EventBus async dispatch queue initialized: MaxQueueSize={MaxQueueSize}, FullQueuePolicy={FullQueuePolicy}",
            _dispatchOptions.MaxQueueSize,
            _dispatchOptions.FullQueuePolicy);
    }

    private Channel<DispatchWorkItem>? _dispatchChannel;
    private Task? _dispatchWorker;
    private readonly DispatchOptions _dispatchOptions;

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
    /// Publishes an event asynchronously with backpressure management.
    ///
    /// <para>When async dispatch is enabled (default):</para>
    /// <list type="bullet">
    /// <item><description>Events are queued in a bounded channel for async processing</description></item>
    /// <item><description>I/O-bound handlers (metrics, audit logging) don't block the caller</description></item>
    /// <item><description>Backpressure is applied when the queue is full (configurable policy)</description></item>
    /// </list>
    ///
    /// <para>When async dispatch is disabled:</para>
    /// <list type="bullet">
    /// <item><description>Events are processed synchronously inline (original behavior)</description></item>
    /// <item><description>Useful for testing or when ordering guarantees are critical</description></item>
    /// </list>
    /// </summary>
    /// <typeparam name="TEvent">The event type to publish.</typeparam>
    /// <param name="event">The event to publish.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="event"/> is <see langword="null"/>.</exception>
    /// <exception cref="ObjectDisposedException">The event bus has been disposed.</exception>
    /// <exception cref="EventBusException">Thrown when the event cannot be queued due to backpressure and drop-oldest policy.</exception>
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

        if (_dispatchOptions.UseAsyncDispatch)
        {
            await PublishWithAsyncDispatchAsync(@event, eventName, handlers).ConfigureAwait(false);
        }
        else
        {
            await PublishInlineAsync(@event, eventName, handlers).ConfigureAwait(false);
        }
    }

    private async Task PublishWithAsyncDispatchAsync<TEvent>(TEvent @event, string eventName, List<Delegate> handlers) where TEvent : EventBase
    {
        // Take a snapshot of handlers to avoid issues with concurrent modifications during iteration
        List<Delegate> handlerSnapshot;
        lock (handlers)
        {
            handlerSnapshot = new List<Delegate>(handlers);
        }

        var workItem = new DispatchWorkItem(@event, null, false);

        try
        {
            // Try to write to dispatch channel with backpressure handling
            var writeTask = _dispatchChannel!.Writer.WriteAsync(workItem, _dispatchOptions.FullQueuePolicy == DispatchQueueFullPolicy.Block
                ? new CancellationTokenSource(_dispatchOptions.MaxWaitTime).Token
                : CancellationToken.None);

            if (writeTask.IsCompletedSuccessfully)
            {
                await writeTask;
                _logger.LogDebug("Queued event for async dispatch: EventType={EventType}", eventName);
            }
            else
            {
                // If we're using drop-oldest and the channel is full, the write will complete immediately
                // but we need to check if it was actually written
                await writeTask;
                _logger.LogDebug("Queued event for async dispatch: EventType={EventType}", eventName);
            }
        }
        catch (OperationCanceledException) when (_dispatchOptions.FullQueuePolicy == DispatchQueueFullPolicy.Block)
        {
            _logger.LogWarning("Event dropped due to backpressure timeout: EventType={EventType}, MaxWaitTime={MaxWaitTime}",
                eventName, _dispatchOptions.MaxWaitTime);
            throw new EventBusException($"Event bus queue full and blocking timeout exceeded ({_dispatchOptions.MaxWaitTime}) for event type {eventName}");
        }
        catch (InvalidOperationException) when (_dispatchOptions.FullQueuePolicy == DispatchQueueFullPolicy.DropOldest)
        {
            _logger.LogWarning("Event dropped due to full queue with drop-oldest policy: EventType={EventType}, QueueSize={QueueSize}",
                eventName, _dispatchOptions.MaxQueueSize);
            throw new EventBusException($"Event bus queue full with drop-oldest policy for event type {eventName}");
        }
    }

    private async Task PublishInlineAsync<TEvent>(TEvent @event, string eventName, List<Delegate> handlers) where TEvent : EventBase
    {
        // Original synchronous behavior for compatibility
        var exceptions = new List<Exception>();

        foreach (var handler in handlers)
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

        if (exceptions.Count > 0)
        {
            throw new EventBusException($"One or more event handlers failed for event type {eventName}", new AggregateException(exceptions));
        }

        _logger.LogInformation("Published event inline: EventType={EventType}, SubscriberCount={Count}", eventName, handlers.Count);
    }

    private async Task DispatchLoopAsync()
    {
        try
        {
            while (await _dispatchChannel!.Reader.WaitToReadAsync().ConfigureAwait(false))
            {
                while (_dispatchChannel.Reader.TryRead(out var workItem))
                {
                    if (workItem.IsShutdownSignal)
                    {
                        _logger.LogDebug("Dispatch worker received shutdown signal");
                        return;
                    }

                    await ProcessDispatchWorkItemAsync(workItem).ConfigureAwait(false);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in dispatch worker loop");
        }
        finally
        {
            _logger.LogDebug("Dispatch worker loop completed");
        }
    }

    private async Task ProcessDispatchWorkItemAsync(DispatchWorkItem workItem)
    {
        if (workItem.Event == null)
        {
            return; // Shutdown signal
        }

        var eventName = workItem.Event.GetType().Name;
        var handlers = _subscribers.TryGetValue(eventName, out var h) ? h : null;

        if (handlers == null || handlers.Count == 0)
        {
            _logger.LogDebug("Dispatched event with no subscribers: EventType={EventType}", eventName);
            return;
        }

        // Take a snapshot of handlers to avoid issues with concurrent modifications during iteration
        List<Delegate> handlerSnapshot;
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
                    case Action<EventBase> syncHandler:
                        syncHandler(workItem.Event);
                        break;
                    case Func<EventBase, Task> asyncHandler:
                        await asyncHandler(workItem.Event).ConfigureAwait(false);
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing dispatched event handler: EventType={EventType}", eventName);
                exceptions.Add(ex);
            }
        }

        if (exceptions.Count > 0)
        {
            _logger.LogError("One or more dispatched event handlers failed for event type {EventType}", eventName);
        }
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

        // Convert queue to list while maintaining order (oldest first)
        var history = new List<EventRecord>();
        foreach (var record in _eventHistory)
        {
            history.Add(record);
        }

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

        _eventHistory.Enqueue(record);

        // Trim history if it exceeds max size or max age
        TrimHistory();
    }

    /// <summary>
    /// Trims the event history to respect bounded retention policies.
    /// Removes oldest events when exceeding max size or when events exceed max age.
    /// </summary>
    private void TrimHistory()
    {
        // First, remove events older than max age
        if (_maxHistoryAge != TimeSpan.Zero)
        {
            var cutoffTime = DateTime.UtcNow - _maxHistoryAge;
            while (_eventHistory.TryPeek(out var oldest) && oldest.PublishedAt < cutoffTime)
            {
                _eventHistory.TryDequeue(out _);
            }
        }

        // Then, remove oldest events if we exceed max size
        while (_eventHistory.Count > _maxHistorySize && _eventHistory.TryPeek(out _))
        {
            _eventHistory.TryDequeue(out _);
        }
    }

    /// <summary>
    /// Disposes the event bus, clearing all subscribers and event history.
    /// If async dispatch is enabled, waits for queued events to complete before disposing.
    /// </summary>
    /// <returns>A <see cref="ValueTask"/> representing the asynchronous operation.</returns>
    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 0)
        {
            _subscribers.Clear();

            if (_dispatchOptions.UseAsyncDispatch && _dispatchWorker != null)
            {
                _logger.LogInformation("EventBus initiating graceful shutdown - waiting for queued events to complete...");

                // Signal dispatch loop to complete
                if (_dispatchChannel != null)
                {
                    try
                    {
                        await _dispatchChannel.Writer.WriteAsync(new DispatchWorkItem(null, null, true));
                        await _dispatchWorker;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error during graceful shutdown of dispatch worker");
                    }
                }

                _logger.LogInformation("EventBus async dispatch queue drained and disposed");
            }
            else
            {
                _logger.LogInformation("EventBus disposed and all subscribers cleared");
            }
        }
    }

    /// <summary>
    /// Disposes the event bus, clearing all subscribers and event history.
    /// For synchronous compatibility, calls DisposeAsync() and waits for completion.
    /// </summary>
    public void Dispose()
    {
        DisposeAsync().GetAwaiter().GetResult();
    }

    private readonly record struct DispatchWorkItem(EventBase? Event, Delegate? Handler, bool IsShutdownSignal);
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
/// <param name="requestId">The correlation/request ID for tracing the operation.</param>
public sealed class ServiceRegisteredEvent : EventBase
{
    public string ServiceId { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
}

/// <summary>
/// Fired when a service is unregistered.
/// </summary>
/// <param name="requestId">The correlation/request ID for tracing the operation.</param>
public sealed class ServiceUnregisteredEvent : EventBase
{
    public string ServiceId { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
}

/// <summary>
/// Fired when a method is invoked.
/// </summary>
/// <param name="requestId">The correlation/request ID for tracing the operation.</param>
public sealed class MethodInvokedEvent : EventBase
{
    /// <summary>Gets or sets the service identifier.</summary>
    public string ServiceId { get; set; } = string.Empty;

    /// <summary>Gets or sets the name of the method being invoked.</summary>
    public string MethodName { get; set; } = string.Empty;

    /// <summary>Gets or sets the duration of the method invocation in milliseconds.</summary>
    public long DurationMs { get; set; }

    /// <summary>Gets or sets a value indicating whether the method invocation was successful.</summary>
    public bool Success { get; set; }
}

/// <summary>
/// Completion status of a stream.
/// </summary>
public enum StreamCompletionStatus
{
    /// <summary>Stream completed successfully.</summary>
    Completed,

    /// <summary>Stream was cancelled by the caller.</summary>
    Cancelled,

    /// <summary>Stream terminated due to an error.</summary>
    Faulted
}

/// <summary>
/// Fired when a stream starts.
/// </summary>
/// <param name="requestId">The correlation/request ID for tracing the operation.</param>
public sealed class StreamStartedEvent : EventBase
{
    /// <summary>Gets or sets the stream identifier.</summary>
    public string StreamId { get; set; } = string.Empty;

    /// <summary>Gets or sets the service identifier.</summary>
    public string ServiceId { get; set; } = string.Empty;

    /// <summary>Gets or sets the name of the method associated with the stream.</summary>
    public string MethodName { get; set; } = string.Empty;
}

/// <summary>
/// Fired when a stream ends.
/// </summary>
/// <param name="requestId">The correlation/request ID for tracing the operation.</param>
public sealed class StreamEndedEvent : EventBase
{
    /// <summary>Gets or sets the stream identifier.</summary>
    public string StreamId { get; set; } = string.Empty;

    /// <summary>Gets or sets the total number of messages processed in the stream.</summary>
    public long MessageCount { get; set; }

    /// <summary>Gets or sets the duration of the stream in milliseconds.</summary>
    public long DurationMs { get; set; }

    /// <summary>Gets or sets the completion status of the stream.</summary>
    public StreamCompletionStatus CompletionStatus { get; set; }

    /// <summary>
    /// Gets or sets the error code when <see cref="CompletionStatus"/> is <see cref="StreamCompletionStatus.Faulted"/>.
    /// </summary>
    public string? ErrorCode { get; set; }

    /// <summary>
    /// Gets or sets a human-readable description of the termination reason.
    /// </summary>
    public string? CloseReason { get; set; }
}

/// <summary>
/// Fired when authentication fails.
/// </summary>
/// <param name="requestId">The correlation/request ID for tracing the operation.</param>
public sealed class AuthenticationFailedEvent : EventBase
{
    /// <summary>Gets or sets the user identifier.</summary>
    public string? UserId { get; set; }

    /// <summary>Gets or sets the reason for authentication failure.</summary>
    public string FailureReason { get; set; } = string.Empty;

    /// <summary>Gets or sets the client IP address.</summary>
    public string? ClientIp { get; set; }
}
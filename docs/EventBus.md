# EventBus

The `EventBus` provides a centralized mechanism for decoupled communication within the `grpc-web-bridge` application by implementing a publish-subscribe pattern. It enables components to register for specific event types, publish events asynchronously, and query event history for diagnostic or auditing purposes, ensuring scalable and maintainable inter-service interactions.

## API

### Constructors
*   `public EventBus()`: Initializes a new instance of the `EventBus`.

### Methods
*   `public void Subscribe<TEvent>(...)`: Registers a handler for a specific event type.
*   `public void Subscribe<TEvent>(...)`: Registers an alternative handler for a specific event type.
*   `public bool Unsubscribe<TEvent>(...)`: Removes the handler for a specific event type. Returns `true` if removed successfully, `false` otherwise.
*   `public async Task PublishAsync<TEvent>(TEvent eventData)`: Asynchronously publishes an event of the specified type to all registered subscribers.
*   `public int GetSubscriberCount<TEvent>()`: Returns the count of registered subscribers for a specified event type.
*   `public List<EventRecord> GetEventHistory()`: Retrieves the historical list of published event records.
*   `public void ClearSubscribers()`: Removes all registered event subscribers.
*   `public void Dispose()`: Performs resource cleanup for the `EventBus` instance.

### Properties
*   `public string EventId`: Gets the identifier for the event.
*   `public DateTime CreatedAt`: Gets the timestamp when the event was created.
*   `public string? Source`: Gets the source identifier of the event, if available.
*   `public Dictionary<string, object>? Metadata`: Gets the associated metadata for the event.
*   `public string EventType`: Gets the type identifier for the event.
*   `public DateTime PublishedAt`: Gets the timestamp when the event was published.
*   `public object? Data`: Gets the underlying event data payload.
*   `public string ServiceId`: Gets the identifier for the service associated with the event.
*   `public string ServiceName`: Gets the name of the service associated with the event.
*   `public string Endpoint`: Gets the endpoint associated with the event.

## Usage

```csharp
// Example 1: Basic Subscription and Publication
var eventBus = new EventBus();
eventBus.Subscribe<OrderPlacedEvent>(e => Console.WriteLine($"Order received: {e.OrderId}"));

await eventBus.PublishAsync(new OrderPlacedEvent { OrderId = "12345" });
```

```csharp
// Example 2: Accessing Event History
var eventBus = new EventBus();
// ... events are published ...

var history = eventBus.GetEventHistory();
foreach (var record in history)
{
    Console.WriteLine($"Event {record.EventId} of type {record.EventType} published at {record.PublishedAt}");
}
```

## Notes

*   **Thread Safety**: The `EventBus` is designed to be thread-safe regarding subscription management and event publication; however, subscribers should ensure that their individual handler implementations are thread-safe if accessing shared state.
*   **Dispose**: The `Dispose` method must be called when the `EventBus` instance is no longer required to free resources properly, particularly if subscribers or event-handling processes hold unmanaged resources.
*   **Event Ordering**: While `PublishAsync` ensures that events are dispatched, the order of execution among multiple subscribers for the same event type is not strictly guaranteed.

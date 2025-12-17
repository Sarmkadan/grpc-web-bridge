# WebhookPublisher

The `WebhookPublisher` class manages the lifecycle and execution of webhook dispatching within the `grpc-web-bridge` system, facilitating the subscription to, and asynchronous delivery of, specified event types to external endpoints. It provides functionality to monitor delivery statistics, manage active subscriptions, and configure retry policies for failed attempts.

## API

### Constructors
- `public WebhookPublisher()`
  Initializes a new instance of the `WebhookPublisher` class.

### Methods
- `public string Subscribe(string endpoint, string[] eventTypes)`
  Registers a new webhook subscription for specified event types at the given endpoint. Returns a unique subscription identifier.
- `public bool Unsubscribe(string subscriptionId)`
  Removes an existing subscription identified by `subscriptionId`. Returns `true` if the subscription was successfully removed, otherwise `false`.
- `public async Task PublishEventAsync(object payload)`
  Asynchronously delivers the specified event payload to all active subscribers.
- `public List<WebhookSubscription> GetSubscriptions()`
  Returns a list of all currently active `WebhookSubscription` objects.
- `public object GetStatistics()`
  Retrieves a summary object containing delivery statistics, including success and failure counts.
- `public void Dispose()`
  Releases all resources used by the `WebhookPublisher`.

### Properties
- `public string Id { get; set; }`
  The unique identifier for the publisher instance.
- `public string Url { get; set; }`
  The target endpoint URL for webhooks.
- `public string[] EventTypes { get; set; }`
  The list of event types this publisher handles.
- `public Dictionary<string, string>? Headers { get; set; }`
  Optional HTTP headers to include with outgoing requests.
- `public bool RetryOnFailure { get; set; }`
  Indicates whether delivery attempts should be retried upon failure.
- `public bool IsActive { get; set; }`
  The current operational status of the publisher.
- `public DateTime CreatedAt { get; set; }`
  The timestamp when this publisher was created.
- `public DateTime? LastSuccessfulDelivery { get; set; }`
  The timestamp of the most recent successful webhook delivery.
- `public long SuccessCount { get; set; }`
  The total number of successful deliveries.
- `public long FailureCount { get; set; }`
  The total number of failed delivery attempts.
- `public string EventId { get; set; }`
  The identifier of the most recently published event.
- `public string EventType { get; set; }`
  The type of the most recently published event.
- `public DateTime Timestamp { get; set; }`
  The timestamp associated with the most recently published event.

## Usage

### Subscribing to Events
```csharp
var publisher = new WebhookPublisher();
string[] events = { "order.created", "order.updated" };
string subscriptionId = publisher.Subscribe("https://api.example.com/webhooks", events);
```

### Publishing an Event
```csharp
var publisher = new WebhookPublisher();
var eventData = new { OrderId = 12345, Status = "Completed" };

await publisher.PublishEventAsync(eventData);

if (publisher.FailureCount > 0)
{
    Console.WriteLine($"Delivery failed for event {publisher.EventId}");
}
```

## Notes

- **Thread Safety**: The `PublishEventAsync` method is thread-safe and supports concurrent invocations. However, modifications to properties like `Headers` or `RetryOnFailure` while events are being published may lead to inconsistent behavior and should be performed during initialization.
- **Disposal**: The `Dispose` method should be called when the `WebhookPublisher` is no longer needed to ensure underlying network resources and internal queues are properly cleaned up.
- **Event State**: Properties `EventId`, `EventType`, and `Timestamp` reflect the state of the *most recent* event processed by the publisher. In highly concurrent scenarios, these values may be overwritten rapidly and should not be relied upon for auditing or transactional integrity.
- **Retry Logic**: When `RetryOnFailure` is enabled, the publisher implements an internal retry mechanism. If delivery consistently fails, the `FailureCount` will increment, and the publisher may eventually mark the subscription as inactive depending on the underlying configuration.

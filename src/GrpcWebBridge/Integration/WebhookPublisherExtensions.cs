#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using GrpcWebBridge.Events;
using GrpcWebBridge.Utilities;

namespace GrpcWebBridge.Integration;

/// <summary>
/// Extension methods for <see cref="WebhookPublisher"/> to provide additional functionality
/// for webhook management, event filtering, and subscription operations.
/// </summary>
public static class WebhookPublisherExtensions
{
    private const int DefaultTimeoutMilliseconds = 30000;
    private const int DefaultMaxRetries = 3;
    private const int DefaultCount = 0;
    private const double DefaultFailureRate = 0.0;

    /// <summary>
    /// Subscribes to specific event types with a callback for matching events.
    /// </summary>
    /// <param name="publisher">The webhook publisher instance.</param>
    /// <param name="webhookUrl">The URL to receive webhooks.</param>
    /// <param name="eventTypeFilter">Filter function to determine which events to send.</param>
    /// <param name="headers">Optional headers to include in webhook requests.</param>
    /// <param name="retryOnFailure">Whether to retry failed deliveries.</param>
    /// <returns>The subscription ID.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="publisher"/> or <paramref name="eventTypeFilter"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="webhookUrl"/> is <see langword="null"/>, empty, or consists only of whitespace.</exception>
    /// <exception cref="InvalidOperationException">Thrown when no event types match the provided filter.</exception>
    public static string SubscribeWithFilter(
        this WebhookPublisher publisher,
        string webhookUrl,
        Func<string, bool> eventTypeFilter,
        Dictionary<string, string>? headers = null,
        bool retryOnFailure = true)
    {
        ArgumentNullException.ThrowIfNull(publisher);
        ArgumentNullException.ThrowIfNull(eventTypeFilter);
        ArgumentException.ThrowIfNullOrWhiteSpace(webhookUrl);

        // Get all known event types from the event bus
        var allEventTypes = new List<string>();

        // Use reflection to find all event types in the Events namespace
        var eventTypes = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .Where(t => t.Namespace == "GrpcWebBridge.Events" &&
                        typeof(EventBase).IsAssignableFrom(t) &&
                        !t.IsAbstract && t != typeof(EventBase))
            .Select(t => t.Name)
            .ToArray();

        allEventTypes.AddRange(eventTypes);

        // Filter to only event types that match our filter
        var matchingEventTypes = allEventTypes
            .Where(eventType => eventTypeFilter(eventType))
            .ToArray();

        if (matchingEventTypes.Length == 0)
        {
            throw new InvalidOperationException(
                "No event types matched the provided filter. Subscription would have no effect.");
        }

        return publisher.Subscribe(webhookUrl, matchingEventTypes, headers, retryOnFailure);
    }

    /// <summary>
    /// Publishes an event to webhooks synchronously (waits for completion).
    /// </summary>
    /// <param name="publisher">The webhook publisher instance.</param>
    /// <param name="event">The event to publish.</param>
    /// <param name="timeoutMilliseconds">Maximum time to wait for delivery (default: 30 seconds).</param>
    /// <returns>Task representing the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="publisher"/> or <paramref name="event"/> is <see langword="null"/>.</exception>
    /// <exception cref="TimeoutException">Thrown when the operation times out before completion.</exception>
    public static async Task PublishEventAsync(
        this WebhookPublisher publisher,
        EventBase @event,
        int timeoutMilliseconds = DefaultTimeoutMilliseconds)
    {
        ArgumentNullException.ThrowIfNull(publisher);
        ArgumentNullException.ThrowIfNull(@event);

        // Create a task completion source to await the event processing
        var tcs = new TaskCompletionSource<bool>();

        // We need to track if the event was successfully queued
        var eventQueued = false;

        // Use reflection to access the internal _eventQueue field
        var eventQueueField = typeof(WebhookPublisher).GetField(
            "_eventQueue",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (eventQueueField != null)
        {
            var eventQueue = (System.Threading.Channels.Channel<WebhookEvent>?)eventQueueField.GetValue(publisher);

            if (eventQueue != null)
            {
                try
                {
                    var webhookEvent = new WebhookEvent
                    {
                        EventId = @event.EventId,
                        EventType = @event.GetType().Name,
                        Timestamp = DateTime.UtcNow,
                        Payload = @event,
                        SubscriptionId = string.Empty, // Will be set by the publisher
                        RetryCount = 0,
                        MaxRetries = DefaultMaxRetries // Default value from WebhookPublisherOptions
                    };

                    await eventQueue.Writer.WriteAsync(webhookEvent).ConfigureAwait(false);
                    eventQueued = true;
                    tcs.TrySetResult(true);
                }
                catch (Exception ex)
                {
                    tcs.TrySetException(ex);
                }
            }
        }

        if (!eventQueued)
        {
            // Fallback to the standard async method
            await publisher.PublishEventAsync(@event).ConfigureAwait(false);
        }

        // Wait for completion with timeout
        using var cts = new CancellationTokenSource(timeoutMilliseconds);
        try
        {
            await tcs.Task.WaitAsync(cts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw new TimeoutException(
                $"Webhook event delivery timed out after {timeoutMilliseconds}ms. " +
                "The event was queued but may take longer to process.");
        }
    }

    /// <summary>
    /// Gets statistics with strongly-typed result for easier consumption.
    /// </summary>
    /// <param name="publisher">The webhook publisher instance.</param>
    /// <returns>Strongly-typed statistics object.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="publisher"/> is <see langword="null"/>.</exception>
    public static WebhookStatistics GetStatisticsTyped(this WebhookPublisher publisher)
    {
        ArgumentNullException.ThrowIfNull(publisher);

        var stats = publisher.GetStatistics();

        if (stats is not null)
        {
            var totalSubscriptionsProp = stats.GetType().GetProperty("totalSubscriptions");
            var activeSubscriptionsProp = stats.GetType().GetProperty("activeSubscriptions");
            var totalEventsSentProp = stats.GetType().GetProperty("totalEventsSent");
            var totalEventsFailedProp = stats.GetType().GetProperty("totalEventsFailed");
            var averageFailureRateProp = stats.GetType().GetProperty("averageFailureRate");

            return new WebhookStatistics
            {
                TotalSubscriptions = totalSubscriptionsProp?.GetValue(stats) is int ts ? ts : DefaultCount,
                ActiveSubscriptions = activeSubscriptionsProp?.GetValue(stats) is int asub ? asub : DefaultCount,
                TotalEventsSent = totalEventsSentProp?.GetValue(stats) is long tes ? tes : DefaultCount,
                TotalEventsFailed = totalEventsFailedProp?.GetValue(stats) is long tef ? tef : DefaultCount,
                AverageFailureRate = averageFailureRateProp?.GetValue(stats) is double afr ? afr : DefaultFailureRate
            };
        }

        return new WebhookStatistics
        {
            TotalSubscriptions = DefaultCount,
            ActiveSubscriptions = DefaultCount,
            TotalEventsSent = DefaultCount,
            TotalEventsFailed = DefaultCount,
            AverageFailureRate = DefaultFailureRate
        };
    }

    /// <summary>
    /// Finds subscriptions by URL pattern or exact match.
    /// </summary>
    /// <param name="publisher">The webhook publisher instance.</param>
    /// <param name="urlPattern">URL to search for (exact match or substring).</param>
    /// <param name="isExactMatch">Whether to perform exact match (default: false).</param>
    /// <returns>List of matching subscriptions.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="publisher"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="urlPattern"/> is <see langword="null"/>, empty, or consists only of whitespace.</exception>
    public static List<WebhookSubscription> FindSubscriptionsByUrl(
        this WebhookPublisher publisher,
        string urlPattern,
        bool isExactMatch = false)
    {
        ArgumentNullException.ThrowIfNull(publisher);
        ArgumentException.ThrowIfNullOrWhiteSpace(urlPattern);

        var allSubscriptions = publisher.GetSubscriptions();

        return isExactMatch
            ? allSubscriptions
                .Where(s => s.Url.Equals(urlPattern, StringComparison.OrdinalIgnoreCase))
                .ToList()
            : allSubscriptions
                .Where(s => s.Url.Contains(urlPattern, StringComparison.OrdinalIgnoreCase))
                .ToList();
    }

    /// <summary>
    /// Strongly-typed statistics object for webhook publisher.
    /// </summary>
    public sealed class WebhookStatistics
    {
        /// <summary>
        /// Total number of subscriptions registered.
        /// </summary>
        public int TotalSubscriptions { get; set; }

        /// <summary>
        /// Number of currently active subscriptions.
        /// </summary>
        public int ActiveSubscriptions { get; set; }

        /// <summary>
        /// Total number of events successfully sent.
        /// </summary>
        public long TotalEventsSent { get; set; }

        /// <summary>
        /// Total number of events that failed to send.
        /// </summary>
        public long TotalEventsFailed { get; set; }

        /// <summary>
        /// Average failure rate as a percentage (0-100).
        /// </summary>
        public double AverageFailureRate { get; set; }

        public override string ToString() =>
            $"Subscriptions: {TotalSubscriptions} (Active: {ActiveSubscriptions}), " +
            $"Events: {TotalEventsSent} sent, {TotalEventsFailed} failed, " +
            $"Failure rate: {AverageFailureRate:P2}";
    }
}

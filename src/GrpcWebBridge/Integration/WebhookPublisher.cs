// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using GrpcWebBridge.Events;
using GrpcWebBridge.Utilities;
using System.Collections.Concurrent;

namespace GrpcWebBridge.Integration;

/// <summary>
/// Publishes events as HTTP webhooks to external endpoints.
/// Enables integration with external systems and monitoring tools.
/// Implements retry logic and event filtering.
/// </summary>
public class WebhookPublisher : IDisposable
{
    private readonly ILogger<WebhookPublisher> _logger;
    private readonly HttpClientFactory _httpClientFactory;
    private readonly ConcurrentDictionary<string, WebhookSubscription> _subscriptions;
    private readonly WebhookPublisherOptions _options;
    private readonly Channel<WebhookEvent> _eventQueue;
    private Task? _processingTask;
    private CancellationTokenSource? _cancellationTokenSource;

    public WebhookPublisher(
        ILogger<WebhookPublisher> logger,
        HttpClientFactory httpClientFactory,
        WebhookPublisherOptions? options = null)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _subscriptions = new ConcurrentDictionary<string, WebhookSubscription>();
        _options = options ?? new WebhookPublisherOptions();
        _eventQueue = Channel.CreateUnbounded<WebhookEvent>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });

        StartProcessing();
    }

    /// <summary>
    /// Subscribes to events and sends them to a webhook URL.
    /// </summary>
    public string Subscribe(
        string webhookUrl,
        string[] eventTypes,
        Dictionary<string, string>? headers = null,
        bool retryOnFailure = true)
    {
        if (string.IsNullOrEmpty(webhookUrl))
            throw new ArgumentException("Webhook URL cannot be null or empty", nameof(webhookUrl));

        // Validate URL
        if (!Uri.TryCreate(webhookUrl, UriKind.Absolute, out var uri))
            throw new InvalidOperationException($"Invalid webhook URL: {webhookUrl}");

        var subscriptionId = Guid.NewGuid().ToString();
        var subscription = new WebhookSubscription
        {
            Id = subscriptionId,
            Url = webhookUrl,
            EventTypes = eventTypes ?? Array.Empty<string>(),
            Headers = headers,
            RetryOnFailure = retryOnFailure,
            CreatedAt = DateTime.UtcNow,
            IsActive = true,
            FailureCount = 0
        };

        _subscriptions.TryAdd(subscriptionId, subscription);
        _logger.LogInformation(
            "Webhook subscription created: SubscriptionId={Id}, URL={URL}, EventTypes={Types}",
            subscriptionId, webhookUrl, string.Join(",", eventTypes));

        return subscriptionId;
    }

    /// <summary>
    /// Unsubscribes a webhook.
    /// </summary>
    public bool Unsubscribe(string subscriptionId)
    {
        if (_subscriptions.TryRemove(subscriptionId, out var subscription))
        {
            _logger.LogInformation("Webhook subscription removed: SubscriptionId={Id}", subscriptionId);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Publishes an event to all matching webhook subscriptions.
    /// </summary>
    public async Task PublishEventAsync(EventBase @event)
    {
        if (@event == null)
            throw new ArgumentNullException(nameof(@event));

        var eventType = @event.GetType().Name;
        var matchingSubscriptions = _subscriptions.Values
            .Where(s => s.IsActive && (s.EventTypes.Length == 0 || s.EventTypes.Contains(eventType)))
            .ToList();

        if (matchingSubscriptions.Count == 0)
        {
            _logger.LogDebug("No webhook subscriptions found for event type: {EventType}", eventType);
            return;
        }

        foreach (var subscription in matchingSubscriptions)
        {
            var webhookEvent = new WebhookEvent
            {
                EventId = @event.EventId,
                EventType = eventType,
                Timestamp = DateTime.UtcNow,
                Payload = @event,
                SubscriptionId = subscription.Id,
                RetryCount = 0,
                MaxRetries = _options.MaxRetries
            };

            try
            {
                await _eventQueue.Writer.WriteAsync(webhookEvent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to queue webhook event: EventType={EventType}", eventType);
            }
        }
    }

    /// <summary>
    /// Gets all active webhook subscriptions.
    /// </summary>
    public List<WebhookSubscription> GetSubscriptions()
    {
        return _subscriptions.Values.Where(s => s.IsActive).ToList();
    }

    /// <summary>
    /// Gets statistics about webhook publishing.
    /// </summary>
    public object GetStatistics()
    {
        var subscriptions = _subscriptions.Values.ToList();
        return new
        {
            totalSubscriptions = subscriptions.Count,
            activeSubscriptions = subscriptions.Count(s => s.IsActive),
            totalEventsSent = subscriptions.Sum(s => s.SuccessCount),
            totalEventsFailed = subscriptions.Sum(s => s.FailureCount),
            averageFailureRate = subscriptions.Count > 0
                ? Math.Round(subscriptions.Average(s => s.FailureCount / (double)(s.SuccessCount + s.FailureCount + 1)), 2)
                : 0
        };
    }

    /// <summary>
    /// Starts background processing of webhook events.
    /// </summary>
    private void StartProcessing()
    {
        _cancellationTokenSource = new CancellationTokenSource();
        _processingTask = Task.Run(() => ProcessWebhookEventsAsync(_cancellationTokenSource.Token));
        _logger.LogInformation("Webhook publisher processing started");
    }

    /// <summary>
    /// Processes queued webhook events.
    /// </summary>
    private async Task ProcessWebhookEventsAsync(CancellationToken cancellationToken)
    {
        try
        {
            await foreach (var webhookEvent in _eventQueue.Reader.ReadAllAsync(cancellationToken))
            {
                try
                {
                    if (_subscriptions.TryGetValue(webhookEvent.SubscriptionId, out var subscription))
                    {
                        await SendWebhookAsync(webhookEvent, subscription, cancellationToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing webhook event: EventId={EventId}", webhookEvent.EventId);
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Webhook publisher processing stopped");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error in webhook processor");
        }
    }

    /// <summary>
    /// Sends a webhook to the subscription URL with retry logic.
    /// </summary>
    private async Task SendWebhookAsync(
        WebhookEvent webhookEvent,
        WebhookSubscription subscription,
        CancellationToken cancellationToken)
    {
        int retryCount = 0;
        int maxRetries = subscription.RetryOnFailure ? _options.MaxRetries : 0;

        while (retryCount <= maxRetries)
        {
            try
            {
                var payload = new
                {
                    webhookEvent.EventId,
                    webhookEvent.EventType,
                    webhookEvent.Timestamp,
                    webhookEvent.Payload
                };

                var json = JsonUtility.Serialize(payload);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                var response = await _httpClientFactory.SendAsync(
                    subscription.Url,
                    HttpMethod.Post,
                    content,
                    subscription.Headers,
                    "webhook");

                if (response.IsSuccessStatusCode)
                {
                    subscription.SuccessCount++;
                    subscription.LastSuccessfulDelivery = DateTime.UtcNow;
                    _logger.LogDebug("Webhook delivered successfully: SubscriptionId={Id}, EventType={Type}",
                        subscription.Id, webhookEvent.EventType);
                    return;
                }
                else if (response.StatusCode >= System.Net.HttpStatusCode.InternalServerError)
                {
                    // Retry on 5xx errors
                    if (retryCount < maxRetries)
                    {
                        var delayMs = (int)(Math.Pow(2, retryCount) * 1000); // Exponential backoff
                        await Task.Delay(delayMs, cancellationToken);
                        retryCount++;
                        continue;
                    }
                }

                // Non-retryable error
                throw new HttpRequestException($"Webhook request failed with status {response.StatusCode}");
            }
            catch (Exception ex)
            {
                subscription.FailureCount++;
                _logger.LogWarning(ex, "Webhook delivery failed: SubscriptionId={Id}, Retry={Retry}/{Max}",
                    subscription.Id, retryCount, maxRetries);

                if (retryCount >= maxRetries)
                {
                    // Mark subscription as inactive after too many failures
                    if (subscription.FailureCount > _options.FailureThresholdForDisable)
                    {
                        subscription.IsActive = false;
                        _logger.LogError("Webhook subscription disabled due to excessive failures: SubscriptionId={Id}",
                            subscription.Id);
                    }

                    break;
                }

                retryCount++;
                if (retryCount <= maxRetries)
                {
                    var delayMs = (int)(Math.Pow(2, retryCount - 1) * 1000);
                    await Task.Delay(delayMs, cancellationToken);
                }
            }
        }
    }

    public void Dispose()
    {
        _cancellationTokenSource?.Cancel();
        _processingTask?.Wait(TimeSpan.FromSeconds(5));
        _eventQueue?.Writer.TryComplete();
        _cancellationTokenSource?.Dispose();
        _logger.LogInformation("Webhook publisher disposed");
    }
}

/// <summary>
/// Webhook subscription record.
/// </summary>
public class WebhookSubscription
{
    public string Id { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string[] EventTypes { get; set; } = Array.Empty<string>();
    public Dictionary<string, string>? Headers { get; set; }
    public bool RetryOnFailure { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastSuccessfulDelivery { get; set; }
    public long SuccessCount { get; set; }
    public long FailureCount { get; set; }
}

/// <summary>
/// Internal webhook event.
/// </summary>
public class WebhookEvent
{
    public string EventId { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public object? Payload { get; set; }
    public string SubscriptionId { get; set; } = string.Empty;
    public int RetryCount { get; set; }
    public int MaxRetries { get; set; }
}

/// <summary>
/// Configuration options for webhook publisher.
/// </summary>
public class WebhookPublisherOptions
{
    public int MaxRetries { get; set; } = 3;
    public int FailureThresholdForDisable { get; set; } = 10;
}

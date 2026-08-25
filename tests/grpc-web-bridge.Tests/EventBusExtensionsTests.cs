#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using FluentAssertions;
using GrpcWebBridge.Events;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace GrpcWebBridge.Tests;

/// <summary>
/// Unit tests for the <see cref="EventBus"/> extension methods.
/// </summary>
public sealed class EventBusExtensionsTests : IDisposable
{
    private readonly ILogger<EventBus> _mockLogger;
    private readonly EventBus _eventBus;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventBusExtensionsTests"/> class.
    /// Sets up a mock logger and an <see cref="EventBus"/> instance with a history size of 100.
    /// </summary>
    public EventBusExtensionsTests()
    {
        _mockLogger = Substitute.For<ILogger<EventBus>>();
        _eventBus = new EventBus(_mockLogger, maxHistorySize: 100);
        _mockLogger.LogInformation("EventBusExtensionsTests constructor invoked.");
    }

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// Disposes the <see cref="EventBus"/> instance if it has not already been disposed.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        _mockLogger.LogInformation("Disposing EventBusExtensionsTests.");
        _eventBus.Dispose();
        _mockLogger.LogInformation("Disposed EventBusExtensionsTests.");
        _disposed = true;
    }

    /// <summary>
    /// Verifies that <see cref="EventBusExtensions.HasSubscribers{TEvent}"/> returns false when no subscribers are registered for the specified event type.
    /// </summary>
    [Fact]
    public void HasSubscribers_WithNoSubscribers_ReturnsFalse()
    {
        _mockLogger.LogInformation("Beginning test {TestMethod}", nameof(HasSubscribers_WithNoSubscribers_ReturnsFalse));
        // Act
        var result = _eventBus.HasSubscribers<ServiceRegisteredEvent>();

        // Assert
        result.Should().BeFalse("because no subscribers have been registered");
        _mockLogger.LogInformation("Completed test {TestMethod}", nameof(HasSubscribers_WithNoSubscribers_ReturnsFalse));
    }

    /// <summary>
    /// Verifies that <see cref="EventBusExtensions.HasSubscribers{TEvent}"/> returns true when a single subscriber is registered for the specified event type.
    /// </summary>
    [Fact]
    public void HasSubscribers_WithSingleSubscriber_ReturnsTrue()
    {
        _mockLogger.LogInformation("Beginning test {TestMethod}", nameof(HasSubscribers_WithSingleSubscriber_ReturnsTrue));
        // Arrange
        Action<ServiceRegisteredEvent> handler = _ => {};
        _eventBus.Subscribe(handler);

        // Act
        var result = _eventBus.HasSubscribers<ServiceRegisteredEvent>();

        // Assert
        result.Should().BeTrue("because a subscriber has been registered");
        _mockLogger.LogInformation("Completed test {TestMethod}", nameof(HasSubscribers_WithSingleSubscriber_ReturnsTrue));
    }

    /// <summary>
    /// Verifies that <see cref="EventBusExtensions.HasSubscribers{TEvent}"/> returns true when multiple subscribers are registered for the specified event type.
    /// </summary>
    [Fact]
    public void HasSubscribers_WithMultipleSubscribers_ReturnsTrue()
    {
        _mockLogger.LogInformation("Beginning test {TestMethod}", nameof(HasSubscribers_WithMultipleSubscribers_ReturnsTrue));
        // Arrange
        Action<ServiceRegisteredEvent> handler1 = _ => {};
        Action<ServiceRegisteredEvent> handler2 = _ => {};
        _eventBus.Subscribe(handler1);
        _eventBus.Subscribe(handler2);

        // Act
        var result = _eventBus.HasSubscribers<ServiceRegisteredEvent>();

        // Assert
        result.Should().BeTrue("because multiple subscribers have been registered");
        _mockLogger.LogInformation("Completed test {TestMethod}", nameof(HasSubscribers_WithMultipleSubscribers_ReturnsTrue));
    }

    /// <summary>
    /// Verifies that <see cref="EventBusExtensions.HasSubscribers{TEvent}"/> returns false when subscribers are registered for a different event type than queried.
    /// </summary>
    [Fact]
    public void HasSubscribers_WithDifferentEventType_ReturnsFalse()
    {
        _mockLogger.LogInformation("Beginning test {TestMethod}", nameof(HasSubscribers_WithDifferentEventType_ReturnsFalse));
        // Arrange
        Action<ServiceRegisteredEvent> handler = _ => {};
        _eventBus.Subscribe(handler);

        // Act
        var result = _eventBus.HasSubscribers<ServiceUnregisteredEvent>();

        // Assert
        result.Should().BeFalse("because we only subscribed to ServiceRegisteredEvent");
        _mockLogger.LogInformation("Completed test {TestMethod}", nameof(HasSubscribers_WithDifferentEventType_ReturnsFalse));
    }

    /// <summary>
    /// Verifies that calling <see cref="EventBusExtensions.HasSubscribers{TEvent}"/> on a null <see cref="EventBus"/> instance throws an <see cref="ArgumentNullException"/>.
    /// </summary>
    [Fact]
    public void HasSubscribers_WithNullBus_ThrowsArgumentNullException()
    {
        _mockLogger.LogInformation("Beginning test {TestMethod}", nameof(HasSubscribers_WithNullBus_ThrowsArgumentNullException));
        // Arrange
        EventBus? nullBus = null;

        // Act
        Action act = () => nullBus!.HasSubscribers<ServiceRegisteredEvent>();

        // Assert
        act.Should().Throw<ArgumentNullException>("because null buses are not allowed");
        _mockLogger.LogInformation("Completed test {TestMethod}", nameof(HasSubscribers_WithNullBus_ThrowsArgumentNullException));
    }

    /// <summary>
    /// Verifies that <see cref="EventBusExtensions.PublishIfHasSubscribersAsync{TEvent}"/> does not invoke handlers when no subscribers are registered for the event type.
    /// </summary>
    [Fact]
    public async Task PublishIfHasSubscribersAsync_WithNoSubscribers_DoesNotPublish()
    {
        _mockLogger.LogInformation("Beginning test {TestMethod}", nameof(PublishIfHasSubscribersAsync_WithNoSubscribers_DoesNotPublish));
        // Arrange
        var testEvent = new ServiceRegisteredEvent
        {
            ServiceId = "test-service",
            ServiceName = "Test Service",
            Endpoint = "https://test.example.com"
        };

        // Track if handler was called
        bool handlerCalled = false;
        Action<ServiceRegisteredEvent> handler = _ => handlerCalled = true;
        _eventBus.Subscribe(handler);
        _eventBus.Unsubscribe<ServiceRegisteredEvent>(handler); // Unsubscribe to have no subscribers

        // Act
        await _eventBus.PublishIfHasSubscribersAsync(testEvent);

        // Assert
        handlerCalled.Should().BeFalse("because there are no subscribers for this event type");
        _mockLogger.LogInformation("Completed test {TestMethod}", nameof(PublishIfHasSubscribersAsync_WithNoSubscribers_DoesNotPublish));
    }

    /// <summary>
    /// Verifies that <see cref="EventBusExtensions.PublishIfHasSubscribersAsync{TEvent}"/> invokes the handler and passes the correct event data when a subscriber is registered.
    /// </summary>
    [Fact]
    public async Task PublishIfHasSubscribersAsync_WithSingleSubscriber_PublishesEvent()
    {
        _mockLogger.LogInformation("Beginning test {TestMethod}", nameof(PublishIfHasSubscribersAsync_WithSingleSubscriber_PublishesEvent));
        // Arrange
        var testEvent = new ServiceRegisteredEvent
        {
            ServiceId = "test-service-2",
            ServiceName = "Test Service 2",
            Endpoint = "https://test2.example.com"
        };

        // Track if handler was called
        bool handlerCalled = false;
        ServiceRegisteredEvent? capturedEvent = null;
        Action<ServiceRegisteredEvent> handler = e =>
        {
            handlerCalled = true;
            capturedEvent = e;
        };
        _eventBus.Subscribe(handler);

        // Act
        await _eventBus.PublishIfHasSubscribersAsync(testEvent);

        // Assert
        handlerCalled.Should().BeTrue("because there is a subscriber for this event type");
        capturedEvent.Should().NotBeNull();
        capturedEvent!.ServiceId.Should().Be(testEvent.ServiceId);
        capturedEvent.ServiceName.Should().Be(testEvent.ServiceName);
        capturedEvent.Endpoint.Should().Be(testEvent.Endpoint);
        _mockLogger.LogInformation("Completed test {TestMethod}", nameof(PublishIfHasSubscribersAsync_WithSingleSubscriber_PublishesEvent));
    }

    /// <summary>
    /// Verifies that <see cref="EventBusExtensions.PublishIfHasSubscribersAsync{TEvent}"/> correctly handles and awaits asynchronous handlers.
    /// </summary>
    [Fact]
    public async Task PublishIfHasSubscribersAsync_WithAsyncSubscriber_PublishesEvent()
    {
        _mockLogger.LogInformation("Beginning test {TestMethod}", nameof(PublishIfHasSubscribersAsync_WithAsyncSubscriber_PublishesEvent));
        // Arrange
        var testEvent = new MethodInvokedEvent
        {
            ServiceId = "test-service-3",
            MethodName = "TestMethod",
            DurationMs = 123,
            Success = true
        };

        // Track if handler was called
        bool handlerCalled = false;
        MethodInvokedEvent? capturedEvent = null;
        Func<MethodInvokedEvent, Task> handler = async e =>
        {
            await Task.Delay(10); // Simulate async work
            handlerCalled = true;
            capturedEvent = e;
        };
        _eventBus.Subscribe(handler);

        // Act
        await _eventBus.PublishIfHasSubscribersAsync(testEvent);

        // Assert
        handlerCalled.Should().BeTrue("because the async subscriber should be called");
        capturedEvent.Should().NotBeNull();
        capturedEvent!.ServiceId.Should().Be(testEvent.ServiceId);
        capturedEvent.MethodName.Should().Be(testEvent.MethodName);
        _mockLogger.LogInformation("Completed test {TestMethod}", nameof(PublishIfHasSubscribersAsync_WithAsyncSubscriber_PublishesEvent));
    }

    /// <summary>
    /// Verifies that calling <see cref="EventBusExtensions.PublishIfHasSubscribersAsync{TEvent}"/> on a null <see cref="EventBus"/> instance throws an <see cref="ArgumentNullException"/>.
    /// </summary>
    [Fact]
    public async Task PublishIfHasSubscribersAsync_WithNullBus_ThrowsArgumentNullException()
    {
        _mockLogger.LogInformation("Beginning test {TestMethod}", nameof(PublishIfHasSubscribersAsync_WithNullBus_ThrowsArgumentNullException));
        // Arrange
        EventBus? nullBus = null;
        var testEvent = new ServiceRegisteredEvent();

        // Act
        Func<Task> act = async () => await nullBus!.PublishIfHasSubscribersAsync(testEvent);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>("because null buses are not allowed");
        _mockLogger.LogInformation("Completed test {TestMethod}", nameof(PublishIfHasSubscribersAsync_WithNullBus_ThrowsArgumentNullException));
    }

    /// <summary>
    /// Verifies that calling <see cref="EventBusExtensions.PublishIfHasSubscribersAsync{TEvent}"/> with a null event throws an <see cref="ArgumentNullException"/>.
    /// </summary>
    [Fact]
    public async Task PublishIfHasSubscribersAsync_WithNullEvent_ThrowsArgumentNullException()
    {
        _mockLogger.LogInformation("Beginning test {TestMethod}", nameof(PublishIfHasSubscribersAsync_WithNullEvent_ThrowsArgumentNullException));
        // Arrange
        ServiceRegisteredEvent? nullEvent = null;

        // Act
        Func<Task> act = async () => await _eventBus.PublishIfHasSubscribersAsync(nullEvent!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>("because null events are not allowed");
        _mockLogger.LogInformation("Completed test {TestMethod}", nameof(PublishIfHasSubscribersAsync_WithNullEvent_ThrowsArgumentNullException));
    }

    /// <summary>
    /// Verifies that <see cref="EventBusExtensions.GetEventHistoryJson"/> returns an empty JSON array when the event history is empty.
    /// </summary>
    [Fact]
    public void GetEventHistoryJson_WithEmptyHistory_ReturnsEmptyArray()
    {
        _mockLogger.LogInformation("Beginning test {TestMethod}", nameof(GetEventHistoryJson_WithEmptyHistory_ReturnsEmptyArray));
        // Act
        var result = _eventBus.GetEventHistoryJson();

        // Assert
        result.Should().Be("[]", "because the event history is initially empty");
        _mockLogger.LogInformation("Completed test {TestMethod}", nameof(GetEventHistoryJson_WithEmptyHistory_ReturnsEmptyArray));
    }

    /// <summary>
    /// Verifies that <see cref="EventBusExtensions.GetEventHistoryJson"/> returns a valid JSON string containing the event type and ID when the history contains a single event.
    /// </summary>
    [Fact]
    public void GetEventHistoryJson_WithSingleEvent_ReturnsValidJson()
    {
        _mockLogger.LogInformation("Beginning test {TestMethod}", nameof(GetEventHistoryJson_WithSingleEvent_ReturnsValidJson));
        // Arrange
        var testEvent = new AuthenticationFailedEvent
        {
            UserId = "user123",
            FailureReason = "InvalidToken",
            ClientIp = "192.168.1.1"
        };

        // Manually record an event since we need to bypass the conditional publish
        var record = new EventRecord
        {
            EventType = typeof(AuthenticationFailedEvent).Name,
            EventId = testEvent.EventId,
            PublishedAt = DateTime.UtcNow,
            Data = testEvent
        };

        // Use reflection to access the private _eventHistory field
        var historyField = typeof(EventBus).GetField("_eventHistory", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var history = (System.Collections.Concurrent.ConcurrentBag<EventRecord>)historyField!.GetValue(_eventBus)!;
        history.Add(record);

        // Act
        var result = _eventBus.GetEventHistoryJson();

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().Contain(nameof(AuthenticationFailedEvent));
        result.Should().Contain(testEvent.EventId);
        _mockLogger.LogInformation("Completed test {TestMethod}", nameof(GetEventHistoryJson_WithSingleEvent_ReturnsValidJson));
    }

    /// <summary>
    /// Verifies that calling <see cref="EventBusExtensions.GetEventHistoryJson"/> on a null <see cref="EventBus"/> instance throws an <see cref="ArgumentNullException"/>.
    /// </summary>
    [Fact]
    public void GetEventHistoryJson_WithNullBus_ThrowsArgumentNullException()
    {
        _mockLogger.LogInformation("Beginning test {TestMethod}", nameof(GetEventHistoryJson_WithNullBus_ThrowsArgumentNullException));
        // Arrange
        EventBus? nullBus = null;

        // Act
        Action act = () => nullBus!.GetEventHistoryJson();

        // Assert
        act.Should().Throw<ArgumentNullException>("because null buses are not allowed");
        _mockLogger.LogInformation("Completed test {TestMethod}", nameof(GetEventHistoryJson_WithNullBus_ThrowsArgumentNullException));
    }

    /// <summary>
    /// Verifies that <see cref="EventBus.Reset"/> clears all registered subscribers for a specific event type.
    /// </summary>
    [Fact]
    public void Reset_WithSubscribers_ClearsSubscribers()
    {
        _mockLogger.LogInformation("Beginning test {TestMethod}", nameof(Reset_WithSubscribers_ClearsSubscribers));
        // Arrange
        Action<ServiceRegisteredEvent> handler = _ => {};
        _eventBus.Subscribe(handler);

        // Verify subscribers exist
        _eventBus.HasSubscribers<ServiceRegisteredEvent>().Should().BeTrue();

        // Act
        _eventBus.Reset();

        // Assert
        _eventBus.HasSubscribers<ServiceRegisteredEvent>().Should().BeFalse("because Reset should clear all subscribers");
        _mockLogger.LogInformation("Completed test {TestMethod}", nameof(Reset_WithSubscribers_ClearsSubscribers));
    }

    /// <summary>
    /// Verifies that <see cref="EventBus.Reset"/> clears all registered subscribers across multiple event types.
    /// </summary>
    [Fact]
    public void Reset_WithMultipleEventTypes_ClearsAllSubscribers()
    {
        _mockLogger.LogInformation("Beginning test {TestMethod}", nameof(Reset_WithMultipleEventTypes_ClearsAllSubscribers));
        // Arrange
        Action<ServiceRegisteredEvent> handler1 = _ => {};
        Action<ServiceUnregisteredEvent> handler2 = _ => {};
        Action<MethodInvokedEvent> handler3 = _ => {};
        _eventBus.Subscribe(handler1);
        _eventBus.Subscribe(handler2);
        _eventBus.Subscribe(handler3);

        // Verify all have subscribers
        _eventBus.HasSubscribers<ServiceRegisteredEvent>().Should().BeTrue();
        _eventBus.HasSubscribers<ServiceUnregisteredEvent>().Should().BeTrue();
        _eventBus.HasSubscribers<MethodInvokedEvent>().Should().BeTrue();

        // Act
        _eventBus.Reset();

        // Assert
        _eventBus.HasSubscribers<ServiceRegisteredEvent>().Should().BeFalse();
        _eventBus.HasSubscribers<ServiceUnregisteredEvent>().Should().BeFalse();
        _eventBus.HasSubscribers<MethodInvokedEvent>().Should().BeFalse();
        _mockLogger.LogInformation("Completed test {TestMethod}", nameof(Reset_WithMultipleEventTypes_ClearsAllSubscribers));
    }

    /// <summary>
    /// Verifies that calling <see cref="EventBus.Reset"/> on a null <see cref="EventBus"/> instance throws an <see cref="ArgumentNullException"/>.
    /// </summary>
    [Fact]
    public void Reset_WithNullBus_ThrowsArgumentNullException()
    {
        _mockLogger.LogInformation("Beginning test {TestMethod}", nameof(Reset_WithNullBus_ThrowsArgumentNullException));
        // Arrange
        EventBus? nullBus = null;

        // Act
        Action act = () => nullBus!.Reset();

        // Assert
        act.Should().Throw<ArgumentNullException>("because null buses are not allowed");
        _mockLogger.LogInformation("Completed test {TestMethod}", nameof(Reset_WithNullBus_ThrowsArgumentNullException));
    }

    /// <summary>
    /// Verifies that <see cref="EventBus.Reset"/> executes successfully without throwing when no subscribers are registered.
    /// </summary>
    [Fact]
    public void Reset_WithNoSubscribers_DoesNotThrow()
    {
        _mockLogger.LogInformation("Beginning test {TestMethod}", nameof(Reset_WithNoSubscribers_DoesNotThrow));
        // Act
        Action act = () => _eventBus.Reset();

        // Assert
        act.Should().NotThrow("because Reset should handle empty subscriber collections gracefully");
        _mockLogger.LogInformation("Completed test {TestMethod}", nameof(Reset_WithNoSubscribers_DoesNotThrow));
    }
}

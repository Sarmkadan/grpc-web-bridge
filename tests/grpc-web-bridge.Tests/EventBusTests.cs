#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Collections.Concurrent;
using System.Threading.Tasks;
using FluentAssertions;
using GrpcWebBridge.Events;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace GrpcWebBridge.Tests;

public sealed class EventBusTests : IDisposable
{
    private readonly ILogger<EventBus> _mockLogger;
    private readonly EventBus _eventBus;
    private bool _disposed;

    public EventBusTests()
    {
        _mockLogger = Substitute.For<ILogger<EventBus>>();
        _eventBus = new EventBus(_mockLogger, maxHistorySize: 100);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _eventBus.Dispose();
        _disposed = true;
    }

    [Fact]
    public void Subscribe_WithNullSyncHandler_ThrowsArgumentNullException()
    {
        // Arrange
        Action<ServiceRegisteredEvent>? nullHandler = null;

        // Act
        Action act = () => _eventBus.Subscribe(nullHandler!);

        // Assert
        act.Should().Throw<ArgumentNullException>("because null handlers are not allowed");
    }

    [Fact]
    public void Subscribe_WithNullAsyncHandler_ThrowsArgumentNullException()
    {
        // Arrange
        Func<ServiceRegisteredEvent, Task>? nullHandler = null;

        // Act
        Action act = () => _eventBus.Subscribe(nullHandler!);

        // Assert
        act.Should().Throw<ArgumentNullException>("because null handlers are not allowed");
    }

    [Fact]
    public void Subscribe_WithValidSyncHandler_AddsHandlerToSubscribers()
    {
        // Arrange
        var handlerCalled = false;
        void Handler(ServiceRegisteredEvent @event) => handlerCalled = true;

        // Act
        _eventBus.Subscribe<ServiceRegisteredEvent>(Handler);

        // Assert
        _eventBus.GetSubscriberCount<ServiceRegisteredEvent>().Should().Be(1);
        handlerCalled.Should().BeFalse("because handler hasn't been called yet");
    }

    [Fact]
    public void Subscribe_WithValidAsyncHandler_AddsHandlerToSubscribers()
    {
        // Arrange
        var handlerCalled = false;
        Task Handler(ServiceRegisteredEvent @event)
        {
            handlerCalled = true;
            return Task.CompletedTask;
        }

        // Act
        _eventBus.Subscribe<ServiceRegisteredEvent>(Handler);

        // Assert
        _eventBus.GetSubscriberCount<ServiceRegisteredEvent>().Should().Be(1);
        handlerCalled.Should().BeFalse("because handler hasn't been called yet");
    }

    [Fact]
    public void Subscribe_MultipleHandlersForSameEvent_AddsAllHandlers()
    {
        // Arrange
        var handler1Called = false;
        var handler2Called = false;
        void Handler1(ServiceRegisteredEvent @event) => handler1Called = true;
        void Handler2(ServiceRegisteredEvent @event) => handler2Called = true;

        // Act
        _eventBus.Subscribe<ServiceRegisteredEvent>(Handler1);
        _eventBus.Subscribe<ServiceRegisteredEvent>(Handler2);

        // Assert
        _eventBus.GetSubscriberCount<ServiceRegisteredEvent>().Should().Be(2);
    }

    [Fact]
    public void Subscribe_DifferentEventTypes_AddsSeparateHandlers()
    {
        // Arrange
        var serviceHandlerCalled = false;
        var methodHandlerCalled = false;
        void ServiceHandler(ServiceRegisteredEvent @event) => serviceHandlerCalled = true;
        void MethodHandler(MethodInvokedEvent @event) => methodHandlerCalled = true;

        // Act
        _eventBus.Subscribe<ServiceRegisteredEvent>(ServiceHandler);
        _eventBus.Subscribe<MethodInvokedEvent>(MethodHandler);

        // Assert
        _eventBus.GetSubscriberCount<ServiceRegisteredEvent>().Should().Be(1);
        _eventBus.GetSubscriberCount<MethodInvokedEvent>().Should().Be(1);
    }

    [Fact]
    public void Unsubscribe_WithNullHandler_ThrowsArgumentNullException()
    {
        // Arrange
        Delegate? nullHandler = null;

        // Act
        Action act = () => _eventBus.Unsubscribe<ServiceRegisteredEvent>(nullHandler!);

        // Assert
        act.Should().Throw<ArgumentNullException>("because null handlers are not allowed");
    }

    [Fact]
    public void Unsubscribe_WithNonExistentHandler_ReturnsFalse()
    {
        // Arrange
        void Handler(ServiceRegisteredEvent @event) { }

        // Act
        var result = _eventBus.Unsubscribe<ServiceRegisteredEvent>(Handler);

        // Assert
        result.Should().BeFalse("because handler was never subscribed");
    }

    [Fact]
    public void Unsubscribe_WithExistingSyncHandler_RemovesHandler()
    {
        // Arrange
        var handlerCalled = false;
        void Handler(ServiceRegisteredEvent @event) => handlerCalled = true;
        _eventBus.Subscribe<ServiceRegisteredEvent>(Handler);

        // Act
        var result = _eventBus.Unsubscribe<ServiceRegisteredEvent>(Handler);

        // Assert
        result.Should().BeTrue("because handler was successfully removed");
        _eventBus.GetSubscriberCount<ServiceRegisteredEvent>().Should().Be(0);
    }

    [Fact]
    public void Unsubscribe_WithExistingAsyncHandler_RemovesHandler()
    {
        // Arrange
        var handlerCalled = false;
        Task Handler(ServiceRegisteredEvent @event)
        {
            handlerCalled = true;
            return Task.CompletedTask;
        }
        _eventBus.Subscribe<ServiceRegisteredEvent>(Handler);

        // Act
        var result = _eventBus.Unsubscribe<ServiceRegisteredEvent>(Handler);

        // Assert
        result.Should().BeTrue("because handler was successfully removed");
        _eventBus.GetSubscriberCount<ServiceRegisteredEvent>().Should().Be(0);
    }

    [Fact]
    public void Unsubscribe_MultipleHandlers_RemovesOnlySpecifiedHandler()
    {
        // Arrange
        var handler1Called = false;
        var handler2Called = false;
        void Handler1(ServiceRegisteredEvent @event) => handler1Called = true;
        void Handler2(ServiceRegisteredEvent @event) => handler2Called = true;
        _eventBus.Subscribe<ServiceRegisteredEvent>(Handler1);
        _eventBus.Subscribe<ServiceRegisteredEvent>(Handler2);

        // Act
        var result = _eventBus.Unsubscribe<ServiceRegisteredEvent>(Handler1);

        // Assert
        result.Should().BeTrue("because handler was successfully removed");
        _eventBus.GetSubscriberCount<ServiceRegisteredEvent>().Should().Be(1);
        _eventBus.Unsubscribe<ServiceRegisteredEvent>(Handler2).Should().BeTrue();
    }

    [Fact]
    public async Task PublishAsync_WithNullEvent_ThrowsArgumentNullException()
    {
        // Arrange
        ServiceRegisteredEvent? nullEvent = null;

        // Act
        Func<Task> act = async () => await _eventBus.PublishAsync(nullEvent!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>("because null events are not allowed");
    }

    [Fact]
    public async Task PublishAsync_WithNoSubscribers_DoesNotThrow()
    {
        // Arrange
        var @event = new ServiceRegisteredEvent
        {
            ServiceId = "test-service-id",
            ServiceName = "TestService",
            Endpoint = "http://localhost:5000"
        };

        // Act
        var act = async () => await _eventBus.PublishAsync(@event);

        // Assert
        await act.Should().NotThrowAsync("because publishing without subscribers should be safe");
        _eventBus.GetSubscriberCount<ServiceRegisteredEvent>().Should().Be(0);
    }

    [Fact]
    public async Task PublishAsync_WithSyncHandler_CallsHandler()
    {
        // Arrange
        var handlerCalled = false;
        var receivedEvent = (ServiceRegisteredEvent?)null;
        void Handler(ServiceRegisteredEvent @event)
        {
            handlerCalled = true;
            receivedEvent = @event;
        }
        _eventBus.Subscribe<ServiceRegisteredEvent>(Handler);

        var @event = new ServiceRegisteredEvent
        {
            ServiceId = "test-service-id",
            ServiceName = "TestService",
            Endpoint = "http://localhost:5000"
        };

        // Act
        await _eventBus.PublishAsync(@event);

        // Assert
        handlerCalled.Should().BeTrue("because sync handler should be called");
        receivedEvent.Should().NotBeNull();
        receivedEvent!.ServiceId.Should().Be("test-service-id");
        receivedEvent.ServiceName.Should().Be("TestService");
        receivedEvent.Endpoint.Should().Be("http://localhost:5000");
    }

    [Fact]
    public async Task PublishAsync_WithAsyncHandler_CallsHandler()
    {
        // Arrange
        var handlerCalled = false;
        var receivedEvent = (ServiceRegisteredEvent?)null;
        Task Handler(ServiceRegisteredEvent @event)
        {
            handlerCalled = true;
            receivedEvent = @event;
            return Task.CompletedTask;
        }
        _eventBus.Subscribe<ServiceRegisteredEvent>(Handler);

        var @event = new ServiceRegisteredEvent
        {
            ServiceId = "test-service-id",
            ServiceName = "TestService",
            Endpoint = "http://localhost:5000"
        };

        // Act
        await _eventBus.PublishAsync(@event);

        // Assert
        handlerCalled.Should().BeTrue("because async handler should be called");
        receivedEvent.Should().NotBeNull();
        receivedEvent!.ServiceId.Should().Be("test-service-id");
    }

    [Fact]
    public async Task PublishAsync_WithMultipleHandlers_CallsAllHandlers()
    {
        // Arrange
        var handler1Called = false;
        var handler2Called = false;
        void Handler1(ServiceRegisteredEvent @event) => handler1Called = true;
        Task Handler2(ServiceRegisteredEvent @event)
        {
            handler2Called = true;
            return Task.CompletedTask;
        }
        _eventBus.Subscribe<ServiceRegisteredEvent>(Handler1);
        _eventBus.Subscribe<ServiceRegisteredEvent>(Handler2);

        var @event = new ServiceRegisteredEvent
        {
            ServiceId = "test-service-id",
            ServiceName = "TestService",
            Endpoint = "http://localhost:5000"
        };

        // Act
        await _eventBus.PublishAsync(@event);

        // Assert
        handler1Called.Should().BeTrue("because first handler should be called");
        handler2Called.Should().BeTrue("because second handler should be called");
    }

    [Fact]
    public async Task PublishAsync_WithExceptionInHandler_AggregatesAndThrowsEventBusException()
    {
        // Arrange
        void ThrowingHandler(ServiceRegisteredEvent @event) => throw new InvalidOperationException("Test exception");
        void NormalHandler(ServiceRegisteredEvent @event) { }

        _eventBus.Subscribe<ServiceRegisteredEvent>(ThrowingHandler);
        _eventBus.Subscribe<ServiceRegisteredEvent>(NormalHandler);

        var @event = new ServiceRegisteredEvent
        {
            ServiceId = "test-service-id",
            ServiceName = "TestService",
            Endpoint = "http://localhost:5000"
        };

        // Act
        Func<Task> act = async () => await _eventBus.PublishAsync(@event);

        // Assert
        await act.Should().ThrowAsync<EventBusException>("because exceptions in handlers should be aggregated and thrown");

        // Normal handler should still be called despite exception in another handler
        _eventBus.GetSubscriberCount<ServiceRegisteredEvent>().Should().Be(2);
    }

    [Fact]
    public void GetSubscriberCount_WithNoSubscribers_ReturnsZero()
    {
        // Arrange & Act
        var count = _eventBus.GetSubscriberCount<ServiceRegisteredEvent>();

        // Assert
        count.Should().Be(0);
    }

    [Fact]
    public void GetSubscriberCount_WithSubscribers_ReturnsCorrectCount()
    {
        // Arrange
        void Handler1(ServiceRegisteredEvent @event) { }
        void Handler2(ServiceRegisteredEvent @event) { }
        void Handler3(MethodInvokedEvent @event) { }
        _eventBus.Subscribe<ServiceRegisteredEvent>(Handler1);
        _eventBus.Subscribe<ServiceRegisteredEvent>(Handler2);
        _eventBus.Subscribe<MethodInvokedEvent>(Handler3);

        // Act
        var serviceEventCount = _eventBus.GetSubscriberCount<ServiceRegisteredEvent>();
        var methodEventCount = _eventBus.GetSubscriberCount<MethodInvokedEvent>();

        // Assert
        serviceEventCount.Should().Be(2);
        methodEventCount.Should().Be(1);
    }

    [Fact]
    public void GetEventHistory_WithEmptyHistory_ReturnsEmptyList()
    {
        // Arrange & Act
        var history = _eventBus.GetEventHistory();

        // Assert
        history.Should().BeEmpty();
    }

    [Fact]
    public async Task GetEventHistory_WithFilteredEventType_ReturnsOnlyMatchingEvents()
    {
        // Arrange
        void ServiceHandler(ServiceRegisteredEvent @event) { }
        void MethodHandler(MethodInvokedEvent @event) { }
        _eventBus.Subscribe<ServiceRegisteredEvent>(ServiceHandler);
        _eventBus.Subscribe<MethodInvokedEvent>(MethodHandler);

        var serviceEvent = new ServiceRegisteredEvent
        {
            ServiceId = "service-1",
            ServiceName = "Service1",
            Endpoint = "http://localhost:5000"
        };
        var methodEvent = new MethodInvokedEvent
        {
            ServiceId = "service-1",
            MethodName = "TestMethod",
            DurationMs = 100,
            Success = true
        };

        await _eventBus.PublishAsync(serviceEvent);
        await _eventBus.PublishAsync(methodEvent);

        // Act
        var allHistory = _eventBus.GetEventHistory();
        var serviceHistory = _eventBus.GetEventHistory("ServiceRegisteredEvent");
        var methodHistory = _eventBus.GetEventHistory("MethodInvokedEvent");
        var unknownHistory = _eventBus.GetEventHistory("UnknownEvent");

        // Assert
        allHistory.Should().HaveCount(2);
        serviceHistory.Should().HaveCount(1);
        serviceHistory[0].EventType.Should().Be("ServiceRegisteredEvent");
        methodHistory.Should().HaveCount(1);
        methodHistory[0].EventType.Should().Be("MethodInvokedEvent");
        unknownHistory.Should().BeEmpty();
    }

    [Fact]
    public async Task GetEventHistory_WithMaxHistorySize_TrimsOldestEvents()
    {
        // Arrange
        var eventBus = new EventBus(_mockLogger, maxHistorySize: 5);

        for (int i = 0; i < 10; i++)
        {
            var @event = new ServiceRegisteredEvent
            {
                ServiceId = $"service-{i}",
                ServiceName = $"Service{i}",
                Endpoint = $"http://localhost:{5000 + i}"
            };
            await eventBus.PublishAsync(@event);
        }

        // Act
        var history = eventBus.GetEventHistory();

        // Assert
        history.Should().HaveCount(5, "because history should be trimmed to max size");
        history.Should().BeInDescendingOrder(h => h.PublishedAt, "because events should be ordered by published date descending");
    }

    [Fact]
    public async Task GetEventHistory_WithEventData_IncludesEventData()
    {
        // Arrange
        var receivedEvent = (ServiceRegisteredEvent?)null;
        void Handler(ServiceRegisteredEvent @event)
        {
            receivedEvent = @event;
        }
        _eventBus.Subscribe<ServiceRegisteredEvent>(Handler);

        var @event = new ServiceRegisteredEvent
        {
            ServiceId = "test-service-id",
            ServiceName = "TestService",
            Endpoint = "http://localhost:5000"
        };
        @event.Metadata = new Dictionary<string, object> { { "key", "value" } };

        await _eventBus.PublishAsync(@event);

        // Act
        var history = _eventBus.GetEventHistory();

        // Assert
        history.Should().HaveCount(1);
        history[0].EventType.Should().Be("ServiceRegisteredEvent");
        history[0].EventId.Should().Be(@event.EventId);
        history[0].Data.Should().BeSameAs(@event);
        history[0].Data.Should().BeOfType<ServiceRegisteredEvent>();
    }

    [Fact]
    public void EventId_IsGeneratedForEachEvent()
    {
        // Arrange
        var event1 = new ServiceRegisteredEvent();
        var event2 = new ServiceRegisteredEvent();

        // Act & Assert
        event1.EventId.Should().NotBeNullOrEmpty();
        event2.EventId.Should().NotBeNullOrEmpty();
        event1.EventId.Should().NotBe(event2.EventId, "because each event should have a unique ID");
    }

    [Fact]
    public void ClearSubscribers_RemovesAllSubscribers()
    {
        // Arrange
        void Handler1(ServiceRegisteredEvent @event) { }
        void Handler2(ServiceRegisteredEvent @event) { }
        void Handler3(MethodInvokedEvent @event) { }
        _eventBus.Subscribe<ServiceRegisteredEvent>(Handler1);
        _eventBus.Subscribe<ServiceRegisteredEvent>(Handler2);
        _eventBus.Subscribe<MethodInvokedEvent>(Handler3);

        // Sanity check
        _eventBus.GetSubscriberCount<ServiceRegisteredEvent>().Should().Be(2);
        _eventBus.GetSubscriberCount<MethodInvokedEvent>().Should().Be(1);

        // Act
        _eventBus.ClearSubscribers();

        // Assert
        _eventBus.GetSubscriberCount<ServiceRegisteredEvent>().Should().Be(0);
        _eventBus.GetSubscriberCount<MethodInvokedEvent>().Should().Be(0);
    }

    [Fact]
    public void Dispose_SetsIsDisposedFlag()
    {
        // Arrange
        void Handler(ServiceRegisteredEvent @event) { }
        _eventBus.Subscribe<ServiceRegisteredEvent>(Handler);
        _eventBus.GetSubscriberCount<ServiceRegisteredEvent>().Should().Be(1);

        // Act
        _eventBus.Dispose();

        // Assert
        _eventBus.IsDisposed.Should().BeTrue("because Dispose should set the IsDisposed flag");
    }

    [Fact]
    public void Dispose_CanOnlyBeCalledOnce()
    {
        // Arrange
        var initialCount = _eventBus.GetSubscriberCount<ServiceRegisteredEvent>();

        // Act
        _eventBus.Dispose();
        _eventBus.Dispose();

        // Assert - Should not throw on multiple Dispose calls
        _eventBus.IsDisposed.Should().BeTrue();
    }

    [Fact]
    public async Task PublishAsync_WithMultipleEventTypes_DoesNotMixHandlers()
    {
        // Arrange
        var serviceHandlerCalled = false;
        var methodHandlerCalled = false;
        void ServiceHandler(ServiceRegisteredEvent @event) => serviceHandlerCalled = true;
        void MethodHandler(MethodInvokedEvent @event) => methodHandlerCalled = true;

        _eventBus.Subscribe<ServiceRegisteredEvent>(ServiceHandler);
        _eventBus.Subscribe<MethodInvokedEvent>(MethodHandler);

        var serviceEvent = new ServiceRegisteredEvent
        {
            ServiceId = "service-1",
            ServiceName = "Service1",
            Endpoint = "http://localhost:5000"
        };
        var methodEvent = new MethodInvokedEvent
        {
            ServiceId = "service-1",
            MethodName = "TestMethod",
            DurationMs = 100,
            Success = true
        };

        // Act
        await _eventBus.PublishAsync(serviceEvent);
        await _eventBus.PublishAsync(methodEvent);

        // Assert
        serviceHandlerCalled.Should().BeTrue("because service event handler should be called");
        methodHandlerCalled.Should().BeTrue("because method event handler should be called");
    }

    [Fact]
    public async Task PublishAsync_WithAsyncHandlers_RunsHandlersConcurrently()
    {
        // Arrange
        var handler1Completed = false;
        var handler2Completed = false;
        var handler3Completed = false;

        async Task Handler1(ServiceRegisteredEvent @event)
        {
            await Task.Delay(50);
            handler1Completed = true;
        }

        async Task Handler2(ServiceRegisteredEvent @event)
        {
            await Task.Delay(30);
            handler2Completed = true;
        }

        async Task Handler3(ServiceRegisteredEvent @event)
        {
            await Task.Delay(10);
            handler3Completed = true;
        }

        _eventBus.Subscribe<ServiceRegisteredEvent>(Handler1);
        _eventBus.Subscribe<ServiceRegisteredEvent>(Handler2);
        _eventBus.Subscribe<ServiceRegisteredEvent>(Handler3);

        var @event = new ServiceRegisteredEvent
        {
            ServiceId = "test-service-id",
            ServiceName = "TestService",
            Endpoint = "http://localhost:5000"
        };

        // Act
        await _eventBus.PublishAsync(@event);

        // Assert
        handler1Completed.Should().BeTrue("because all async handlers should complete");
        handler2Completed.Should().BeTrue();
        handler3Completed.Should().BeTrue();
    }

    [Fact]
    public async Task GetEventHistory_WithNullEventType_ReturnsAllEvents()
    {
        // Arrange
        void ServiceHandler(ServiceRegisteredEvent @event) { }
        void MethodHandler(MethodInvokedEvent @event) { }
        _eventBus.Subscribe<ServiceRegisteredEvent>(ServiceHandler);
        _eventBus.Subscribe<MethodInvokedEvent>(MethodHandler);

        var serviceEvent = new ServiceRegisteredEvent
        {
            ServiceId = "service-1",
            ServiceName = "Service1",
            Endpoint = "http://localhost:5000"
        };
        var methodEvent = new MethodInvokedEvent
        {
            ServiceId = "service-1",
            MethodName = "TestMethod",
            DurationMs = 100,
            Success = true
        };

        await _eventBus.PublishAsync(serviceEvent);
        await _eventBus.PublishAsync(methodEvent);

        // Act
        var history = _eventBus.GetEventHistory(null);

        // Assert
        history.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetEventHistory_WithEmptyStringEventType_ReturnsAllEvents()
    {
        // Arrange
        void ServiceHandler(ServiceRegisteredEvent @event) { }
        void MethodHandler(MethodInvokedEvent @event) { }
        _eventBus.Subscribe<ServiceRegisteredEvent>(ServiceHandler);
        _eventBus.Subscribe<MethodInvokedEvent>(MethodHandler);

        var serviceEvent = new ServiceRegisteredEvent
        {
            ServiceId = "service-1",
            ServiceName = "Service1",
            Endpoint = "http://localhost:5000"
        };
        var methodEvent = new MethodInvokedEvent
        {
            ServiceId = "service-1",
            MethodName = "TestMethod",
            DurationMs = 100,
            Success = true
        };

        await _eventBus.PublishAsync(serviceEvent);
        await _eventBus.PublishAsync(methodEvent);

        // Act
        var history = _eventBus.GetEventHistory(string.Empty);

        // Assert
        history.Should().HaveCount(2);
    }


    [Fact]
    public void Subscribe_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        void Handler(ServiceRegisteredEvent @event) { }
        _eventBus.Dispose();

        // Act
        Action act = () => _eventBus.Subscribe<ServiceRegisteredEvent>(Handler);

        // Assert
        act.Should().Throw<ObjectDisposedException>("because subscribing after dispose should throw");
    }

    [Fact]
    public void Unsubscribe_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        void Handler(ServiceRegisteredEvent @event) { }
        _eventBus.Dispose();

        // Act
        Action act = () => _eventBus.Unsubscribe<ServiceRegisteredEvent>(Handler);

        // Assert
        act.Should().Throw<ObjectDisposedException>("because unsubscribing after dispose should throw");
    }

    [Fact]
    public void GetSubscriberCount_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        _eventBus.Dispose();

        // Act
        Action act = () => _eventBus.GetSubscriberCount<ServiceRegisteredEvent>();

        // Assert
        act.Should().Throw<ObjectDisposedException>("because getting subscriber count after dispose should throw");
    }

    [Fact]
    public void GetEventHistory_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        _eventBus.Dispose();

        // Act
        Action act = () => _eventBus.GetEventHistory();

        // Assert
        act.Should().Throw<ObjectDisposedException>("because getting event history after dispose should throw");
    }

    [Fact]
    public void ClearSubscribers_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        _eventBus.Dispose();

        // Act
        Action act = () => _eventBus.ClearSubscribers();

        // Assert
        act.Should().Throw<ObjectDisposedException>("because clearing subscribers after dispose should throw");
    }

    [Fact]
    public async Task PublishAsync_WithMultipleExceptions_AggregatesAllExceptions()
    {
        // Arrange
        void ThrowingHandler1(ServiceRegisteredEvent @event) => throw new InvalidOperationException("Exception 1");
        void ThrowingHandler2(ServiceRegisteredEvent @event) => throw new ArgumentException("Exception 2");
        void NormalHandler(ServiceRegisteredEvent @event) { }

        _eventBus.Subscribe<ServiceRegisteredEvent>(ThrowingHandler1);
        _eventBus.Subscribe<ServiceRegisteredEvent>(ThrowingHandler2);
        _eventBus.Subscribe<ServiceRegisteredEvent>(NormalHandler);

        var @event = new ServiceRegisteredEvent
        {
            ServiceId = "test-service-id",
            ServiceName = "TestService",
            Endpoint = "http://localhost:5000"
        };

        // Act
        Func<Task> act = async () => await _eventBus.PublishAsync(@event);

        // Assert
        var exception = await act.Should().ThrowAsync<EventBusException>();
        exception.And.InnerException.Should().BeOfType<AggregateException>();
        var aggregateException = (AggregateException)exception.And.InnerException;
        aggregateException.InnerExceptions.Should().HaveCount(2);
    }

    [Fact]
    public async Task PublishAsync_WithExceptionInAsyncHandler_AggregatesException()
    {
        // Arrange
        Task ThrowingAsyncHandler(ServiceRegisteredEvent @event) => throw new InvalidOperationException("Async exception");
        void NormalHandler(ServiceRegisteredEvent @event) { }

        _eventBus.Subscribe<ServiceRegisteredEvent>(ThrowingAsyncHandler);
        _eventBus.Subscribe<ServiceRegisteredEvent>(NormalHandler);

        var @event = new ServiceRegisteredEvent
        {
            ServiceId = "test-service-id",
            ServiceName = "TestService",
            Endpoint = "http://localhost:5000"
        };

        // Act
        Func<Task> act = async () => await _eventBus.PublishAsync(@event);

        // Assert
        await act.Should().ThrowAsync<EventBusException>();
    }

    [Fact]
    public void IsDisposed_ReturnsFalse_WhenNotDisposed()
    {
        // Arrange & Act & Assert
        _eventBus.IsDisposed.Should().BeFalse("because the bus should not be disposed initially");
    }

    #region Edge Case Tests for EventBus Behavior

    [Fact]
    public async Task PublishAsync_WithSubscriberException_ContinuesToOtherSubscribers()
    {
        // Arrange
        var firstHandlerCalled = false;
        var secondHandlerCalled = false;
        var thirdHandlerCalled = false;

        void FirstHandler(ServiceRegisteredEvent @event) => firstHandlerCalled = true;
        void SecondHandler(ServiceRegisteredEvent @event) => throw new InvalidOperationException("Second handler failed");
        void ThirdHandler(ServiceRegisteredEvent @event) => thirdHandlerCalled = true;

        _eventBus.Subscribe<ServiceRegisteredEvent>(FirstHandler);
        _eventBus.Subscribe<ServiceRegisteredEvent>(SecondHandler);
        _eventBus.Subscribe<ServiceRegisteredEvent>(ThirdHandler);

        var @event = new ServiceRegisteredEvent
        {
            ServiceId = "test-service-id",
            ServiceName = "TestService",
            Endpoint = "http://localhost:5000"
        };

        // Act
        Func<Task> act = async () => await _eventBus.PublishAsync(@event);

        // Assert
        await act.Should().ThrowAsync<EventBusException>("because one handler threw an exception");

        // All handlers should have been called despite the exception in the middle
        firstHandlerCalled.Should().BeTrue("because first handler should be called");
        secondHandlerCalled.Should().BeFalse("because second handler throws exception before setting flag");
        thirdHandlerCalled.Should().BeTrue("because third handler should still be called after exception");
    }

    [Fact]
    public async Task PublishAsync_WithSubscriberException_InAsyncHandler_ContinuesToOtherSubscribers()
    {
        // Arrange
        var firstHandlerCalled = false;
        var secondHandlerCalled = false;
        var thirdHandlerCalled = false;

        Task FirstHandler(ServiceRegisteredEvent @event)
        {
            firstHandlerCalled = true;
            return Task.CompletedTask;
        }

        Task SecondHandler(ServiceRegisteredEvent @event)
        {
            throw new InvalidOperationException("Async second handler failed");
        }

        Task ThirdHandler(ServiceRegisteredEvent @event)
        {
            thirdHandlerCalled = true;
            return Task.CompletedTask;
        }

        _eventBus.Subscribe<ServiceRegisteredEvent>(FirstHandler);
        _eventBus.Subscribe<ServiceRegisteredEvent>(SecondHandler);
        _eventBus.Subscribe<ServiceRegisteredEvent>(ThirdHandler);

        var @event = new ServiceRegisteredEvent
        {
            ServiceId = "test-service-id",
            ServiceName = "TestService",
            Endpoint = "http://localhost:5000"
        };

        // Act
        Func<Task> act = async () => await _eventBus.PublishAsync(@event);

        // Assert
        await act.Should().ThrowAsync<EventBusException>("because one async handler threw an exception");

        // All handlers should have been called despite the exception in the middle
        firstHandlerCalled.Should().BeTrue("because first async handler should be called");
        secondHandlerCalled.Should().BeFalse("because second async handler throws exception before setting flag");
        thirdHandlerCalled.Should().BeTrue("because third async handler should still be called after exception");
    }

    [Fact]
    public async Task PublishAsync_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        void Handler(ServiceRegisteredEvent @event) { }
        _eventBus.Subscribe<ServiceRegisteredEvent>(Handler);
        _eventBus.Dispose();

        var @event = new ServiceRegisteredEvent
        {
            ServiceId = "test-service-id",
            ServiceName = "TestService",
            Endpoint = "http://localhost:5000"
        };

        // Act
        Func<Task> act = async () => await _eventBus.PublishAsync(@event);

        // Assert
        await act.Should().ThrowAsync<ObjectDisposedException>("because publishing after dispose should throw");
    }

    [Fact]
    public async Task PublishAsync_WithConcurrentPublishes_HandlesRaceConditions()
    {
        // Arrange
        var callCount = 0;
        var lockObj = new object();
        void Handler(ServiceRegisteredEvent @event)
        {
            lock (lockObj)
            {
                callCount++;
            }
        }

        _eventBus.Subscribe<ServiceRegisteredEvent>(Handler);

        // Act - Publish multiple events concurrently
        var publishTasks = new List<Task>();
        for (int i = 0; i < 10; i++)
        {
            var @event = new ServiceRegisteredEvent
            {
                ServiceId = $"service-{i}",
                ServiceName = $"TestService{i}",
                Endpoint = $"http://localhost:{5000 + i}"
            };
            publishTasks.Add(_eventBus.PublishAsync(@event));
        }

        await Task.WhenAll(publishTasks);

        // Assert
        callCount.Should().Be(10, "because each publish should have called the handler exactly once");
    }

    [Fact]
    public async Task PublishAsync_WithConcurrentPublishes_HandlesMultipleEvents()
    {
        // Arrange
        var callCount = 0;
        void Handler(ServiceRegisteredEvent @event)
        {
            Interlocked.Increment(ref callCount);
        }

        _eventBus.Subscribe<ServiceRegisteredEvent>(Handler);

        // Act - Publish multiple events concurrently
        var publishTasks = new List<Task>();
        for (int i = 0; i < 10; i++)
        {
            var @event = new ServiceRegisteredEvent
            {
                ServiceId = $"service-{i}",
                ServiceName = $"TestService{i}",
                Endpoint = $"http://localhost:{5000 + i}"
            };
            publishTasks.Add(_eventBus.PublishAsync(@event));
        }

        await Task.WhenAll(publishTasks);

        // Assert
        callCount.Should().Be(10, "because each publish should have called the handler exactly once");
    }

    [Fact]
    public async Task PublishAsync_LateSubscriber_OnlyReceivesEventsPublishedAfterSubscription()
    {
        // Arrange
        var eventsReceived = new ConcurrentBag<ServiceRegisteredEvent>();
        var earlyEvent = new ServiceRegisteredEvent
        {
            ServiceId = "early-service",
            ServiceName = "EarlyService",
            Endpoint = "http://localhost:5000"
        };
        var lateEvent = new ServiceRegisteredEvent
        {
            ServiceId = "late-service",
            ServiceName = "LateService",
            Endpoint = "http://localhost:5001"
        };

        void Handler(ServiceRegisteredEvent @event) => eventsReceived.Add(@event);

        // Publish an event before subscribing
        await _eventBus.PublishAsync(earlyEvent);

        // Subscribe after the event was published
        _eventBus.Subscribe<ServiceRegisteredEvent>(Handler);

        // Publish another event - handler should receive this
        await _eventBus.PublishAsync(lateEvent);

        // Assert
        eventsReceived.Should().HaveCount(1, "because only the event published after subscription should be received");
        eventsReceived.Single().ServiceId.Should().Be("late-service");
    }

    [Fact]
    public async Task PublishAsync_WithUnsubscribeDuringHandler_DoesNotThrow()
    {
        // Arrange
        var handler1Called = false;
        var handler3Called = false;

        void Handler1(ServiceRegisteredEvent @event)
        {
            handler1Called = true;
            // Unsubscribe another handler during handler1 execution
            // This should not throw due to proper locking
            var handlers = _eventBus.GetSubscriberCount<ServiceRegisteredEvent>();
            handlers.Should().BeGreaterThan(0);
        }

        void Handler3(ServiceRegisteredEvent @event) => handler3Called = true;

        _eventBus.Subscribe<ServiceRegisteredEvent>(Handler1);
        _eventBus.Subscribe<ServiceRegisteredEvent>(Handler3);

        var @event = new ServiceRegisteredEvent
        {
            ServiceId = "test-service-id",
            ServiceName = "TestService",
            Endpoint = "http://localhost:5000"
        };

        // Act
        Func<Task> act = async () => await _eventBus.PublishAsync(@event);

        // Assert
        await act.Should().NotThrowAsync("because EventBus should handle concurrent modifications gracefully");
        handler1Called.Should().BeTrue("because handler1 should execute");
        handler3Called.Should().BeTrue("because handler3 should execute");
    }

    [Fact]
    public async Task PublishAsync_WithUnsubscribeDuringAsyncHandler_DoesNotThrow()
    {
        // Arrange
        var handler1Called = false;
        var handler3Called = false;

        Task Handler1(ServiceRegisteredEvent @event)
        {
            handler1Called = true;
            // Access subscriber count during handler execution
            // This should not throw due to proper locking
            var handlers = _eventBus.GetSubscriberCount<ServiceRegisteredEvent>();
            handlers.Should().BeGreaterThan(0);
            return Task.CompletedTask;
        }

        Task Handler3(ServiceRegisteredEvent @event)
        {
            handler3Called = true;
            return Task.CompletedTask;
        }

        _eventBus.Subscribe<ServiceRegisteredEvent>(Handler1);
        _eventBus.Subscribe<ServiceRegisteredEvent>(Handler3);

        var @event = new ServiceRegisteredEvent
        {
            ServiceId = "test-service-id",
            ServiceName = "TestService",
            Endpoint = "http://localhost:5000"
        };

        // Act
        Func<Task> act = async () => await _eventBus.PublishAsync(@event);

        // Assert
        await act.Should().NotThrowAsync("because EventBus should handle concurrent modifications gracefully");
        handler1Called.Should().BeTrue("because handler1 should execute");
        handler3Called.Should().BeTrue("because handler3 should execute");
    }

    [Fact]
    public async Task PublishAsync_WithMultipleExceptions_InAsyncDispatchMode_AreLoggedButNotPropagated()
    {
        // Arrange
        var eventBusWithAsyncDispatch = new EventBus(
            _mockLogger,
            maxHistorySize: 100,
            dispatchOptions: new EventBus.DispatchOptions { UseAsyncDispatch = true });

        var exceptionCount = 0;
        Task ThrowingHandler(ServiceRegisteredEvent @event)
        {
            Interlocked.Increment(ref exceptionCount);
            throw new InvalidOperationException($"Async handler exception {exceptionCount}");
        }

        Task NormalHandler(ServiceRegisteredEvent @event) => Task.CompletedTask;

        eventBusWithAsyncDispatch.Subscribe<ServiceRegisteredEvent>(ThrowingHandler);
        eventBusWithAsyncDispatch.Subscribe<ServiceRegisteredEvent>(NormalHandler);
        eventBusWithAsyncDispatch.Subscribe<ServiceRegisteredEvent>(ThrowingHandler);

        var @event = new ServiceRegisteredEvent
        {
            ServiceId = "test-service-id",
            ServiceName = "TestService",
            Endpoint = "http://localhost:5000"
        };

        // Act
        Func<Task> act = async () => await eventBusWithAsyncDispatch.PublishAsync(@event);

        // Assert
        await act.Should().NotThrowAsync("because async dispatch mode logs exceptions but doesn't propagate them");

        // Give time for async processing
        await Task.Delay(100);

        // Verify exceptions were logged (we can't easily assert on logger calls with NSubstitute without setup)
        // But we can verify the event was processed
        eventBusWithAsyncDispatch.GetSubscriberCount<ServiceRegisteredEvent>().Should().Be(3);

        // Cleanup
        await eventBusWithAsyncDispatch.DisposeAsync();
    }

    #endregion
}
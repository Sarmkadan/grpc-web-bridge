#nullable enable

// =============================================================================
// Author: Automated Generation
// Memory leak tests for EventBus and RequestContextManager
// =============================================================================

using System.Threading.Tasks;
using FluentAssertions;
using GrpcWebBridge.Events;
using GrpcWebBridge.Integration;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace GrpcWebBridge.Tests;

public sealed class MemoryLeakTests
{
    [Fact]
    public async Task EventBus_MemoryDoesNotGrow_With100kEvents()
    {
        // Arrange
        var logger = Substitute.For<ILogger<EventBus>>();
        var eventBus = new EventBus(logger, maxHistorySize: 1000, maxHistoryAge: TimeSpan.FromMinutes(5));

        // Act - publish 100k events
        for (int i = 0; i < 100000; i++)
        {
            var @event = new ServiceRegisteredEvent
            {
                ServiceId = $"service-{i}",
                ServiceName = $"Service{i}",
                Endpoint = $"http://localhost:{5000 + i}"
            };
            await eventBus.PublishAsync(@event);
        }

        // Assert - history should be bounded
        var history = eventBus.GetEventHistory();
        history.Should().HaveCountLessOrEqualTo(1000, "because history should be bounded by maxHistorySize");

        // Cleanup
        eventBus.Dispose();
    }

    [Fact]
    public async Task EventBus_MemoryDoesNotGrow_WithOldEvents()
    {
        // Arrange
        var logger = Substitute.For<ILogger<EventBus>>();
        var eventBus = new EventBus(logger, maxHistorySize: 1000, maxHistoryAge: TimeSpan.FromMilliseconds(10));

        // Act - publish events with delay to trigger age-based trimming
        for (int i = 0; i < 2000; i++)
        {
            var @event = new ServiceRegisteredEvent
            {
                ServiceId = $"service-{i}",
                ServiceName = $"Service{i}",
                Endpoint = $"http://localhost:{5000 + i}"
            };
            await eventBus.PublishAsync(@event);
            await Task.Delay(15); // Wait longer than maxHistoryAge to trigger trimming
        }

        // Assert - history should be bounded by age
        var history = eventBus.GetEventHistory();
        history.Should().HaveCountLessOrEqualTo(1000, "because old events should be trimmed");

        // Cleanup
        eventBus.Dispose();
    }

    [Fact]
    public async Task RequestContextManager_MemoryDoesNotGrow_With100kRequests()
    {
        // Arrange
        var logger = Substitute.For<ILogger<RequestContextManager>>();
        var manager = new RequestContextManager(logger);

        // Act - simulate 100k requests
        for (int i = 0; i < 100000; i++)
        {
            var requestId = $"req-{i}";
            var userId = $"user-{i}";

            // Create context
            manager.CreateContext(requestId, userId);

            // Simulate work
            await Task.Delay(1);

            // Clear context (simulating request completion)
            manager.Clear();
        }

        // Assert - no active contexts should remain
        var activeCount = manager.GetActiveContextCount();
        activeCount.Should().Be(0, "because all contexts should be cleared after requests complete");

        // Cleanup
        manager.Dispose();
    }

    [Fact]
    public async Task RequestContextManager_ContextRegistryTracksActiveContexts()
    {
        // Arrange
        var logger = Substitute.For<ILogger<RequestContextManager>>();
        var manager = new RequestContextManager(logger);

        // Act - create contexts in sequence
        var context1 = manager.CreateContext("req-1", "user-1");
        var context2 = manager.CreateContext("req-2", "user-2");

        // Assert - both contexts should be tracked
        var activeContexts = manager.GetActiveContexts();
        activeContexts.Should().HaveCount(2);
        activeContexts.Should().Contain(c => c.RequestId == "req-1");
        activeContexts.Should().Contain(c => c.RequestId == "req-2");

        // Clear first context
        manager.Clear();
        activeContexts = manager.GetActiveContexts();
        activeContexts.Should().HaveCount(1);
        activeContexts.Should().Contain(c => c.RequestId == "req-2");

        // Clear second context
        manager.Clear();
        activeContexts = manager.GetActiveContexts();
        activeContexts.Should().BeEmpty();

        // Cleanup
        manager.Dispose();
    }

    [Fact]
    public async Task RequestContextManager_OrphanedContextsCanBeCleanedUp()
    {
        // Arrange
        var logger = Substitute.For<ILogger<RequestContextManager>>();
        var manager = new RequestContextManager(logger);

        // Act - create contexts but don't clear them (simulating orphaned contexts)
        manager.CreateContext("req-1", "user-1");
        manager.CreateContext("req-2", "user-2");

        // Assert - both contexts should be tracked
        var activeContexts = manager.GetActiveContexts();
        activeContexts.Should().HaveCount(2);

        // Clean up orphaned contexts
        manager.TryRemoveContext("req-1").Should().BeTrue();
        manager.TryRemoveContext("req-2").Should().BeTrue();
        activeContexts = manager.GetActiveContexts();
        activeContexts.Should().BeEmpty();

        // Cleanup
        manager.Dispose();
    }
}
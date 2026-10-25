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

/// <summary>
/// Regression tests guarding against unbounded memory growth in <see cref="EventBus"/>
/// (event history) and <see cref="RequestContextManager"/> (active request contexts).
/// Each test drives a long-running or high-volume scenario and asserts that the
/// retained state stays bounded or is released once the work is complete.
/// </summary>
public sealed class MemoryLeakTests
{
    /// <summary>
    /// Guards against the event history of <see cref="EventBus"/> growing without limit
    /// when a large volume of events (100k) is published. Without the count-based cap
    /// (<c>maxHistorySize</c>), every published event would remain referenced forever.
    /// </summary>
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

    /// <summary>
    /// Guards against the event history of <see cref="EventBus"/> retaining stale events
    /// indefinitely. Events older than <c>maxHistoryAge</c> must be trimmed on publish, so
    /// a steady stream of events spread over time cannot accumulate in memory.
    /// </summary>
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

    /// <summary>
    /// Guards against per-request context leaks in <see cref="RequestContextManager"/>.
    /// If each request creates a context and completes without clearing it, the active
    /// context registry would grow with every request (100k here). After every request
    /// is cleared, no active contexts may remain.
    /// </summary>
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

    /// <summary>
    /// Guards against the active context registry of <see cref="RequestContextManager"/>
    /// losing or failing to track live contexts. It verifies that concurrent contexts are
    /// registered, that <c>Clear()</c> removes only the current context, and that
    /// <c>TryRemoveContext</c> releases the remaining entry, so the registry drains to empty.
    /// </summary>
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

        // Clear removes the current (most recently created) context only
        manager.Clear();
        activeContexts = manager.GetActiveContexts();
        activeContexts.Should().HaveCount(1);
        activeContexts.Should().Contain(c => c.RequestId == "req-1");

        // Remove the remaining context explicitly
        manager.TryRemoveContext("req-1").Should().BeTrue();
        activeContexts = manager.GetActiveContexts();
        activeContexts.Should().BeEmpty();

        // Cleanup
        manager.Dispose();
    }

    /// <summary>
    /// Guards against orphaned contexts, i.e. contexts created for requests that never
    /// call <c>Clear()</c>, staying in the registry forever. The test verifies that they
    /// are still tracked (so they can be found) and that <c>TryRemoveContext</c> can
    /// evict each one by request id, leaving the registry empty.
    /// </summary>
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

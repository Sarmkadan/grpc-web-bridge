#nullable enable
// =============================================================================
// Author: Automated Generation
// =============================================================================

using System.Threading.Tasks;
using FluentAssertions;
using GrpcWebBridge.Integration;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace GrpcWebBridge.Tests;

public sealed class RequestContextManagerTests
{
    private readonly ILogger<RequestContextManager> _mockLogger;
    private readonly RequestContextManager _manager;

    public RequestContextManagerTests()
    {
        _mockLogger = Substitute.For<ILogger<RequestContextManager>>();
        _manager = new RequestContextManager(_mockLogger);
    }

    [Fact]
    public void CreateContext_ShouldInitializeProperties_AndBeActive()
    {
        // Arrange
        var requestId = "req-123";
        var userId = "user-456";

        // Act
        var context = _manager.CreateContext(requestId, userId);

        // Assert
        context.RequestId.Should().Be(requestId);
        context.UserId.Should().Be(userId);
        _manager.IsContextActive().Should().BeTrue();
        _manager.GetRequestId().Should().Be(requestId);
        _manager.GetUserId().Should().Be(userId);
    }

    [Fact]
    public void GetContext_ShouldReturnCurrentContext()
    {
        // Arrange
        var ctx = _manager.CreateContext("id-1");

        // Act
        var retrieved = _manager.GetContext();

        // Assert
        retrieved.Should().NotBeNull();
        retrieved.Should().BeSameAs(ctx);
    }

    [Fact]
    public void SetAndGetMetadata_ShouldStoreAndRetrieveValues()
    {
        // Arrange
        _manager.CreateContext("id-meta");
        const string key = "foo";
        const string value = "bar";

        // Act
        _manager.SetMetadata(key, value);
        var retrieved = _manager.GetMetadata(key);

        // Assert
        retrieved.Should().Be(value);
    }

    [Fact]
    public void GetMetadata_NonExistingKey_ShouldReturnNull()
    {
        // Arrange
        _manager.CreateContext("id-nonexistent");
        // Act
        var result = _manager.GetMetadata("does-not-exist");
        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void SetMetadata_WhenNoContext_ShouldNotThrow()
    {
        // Act
        var act = () => _manager.SetMetadata("key", "value");
        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void RecordElapsedTime_ShouldPopulateEndTime_AndElapsedMilliseconds()
    {
        // Arrange
        var ctx = _manager.CreateContext("id-elapsed");
        // Simulate some delay
        System.Threading.Thread.Sleep(10);

        // Act
        _manager.RecordElapsedTime();

        // Assert
        ctx.EndTime.Should().NotBeNull();
        ctx.ElapsedMilliseconds.Should().BeGreaterOrEqualTo(0);
    }

    [Fact]
    public void Clear_ShouldRemoveContext_AndDeactivate()
    {
        // Arrange
        _manager.CreateContext("id-clear");
        _manager.IsContextActive().Should().BeTrue();

        // Act
        _manager.Clear();

        // Assert
        _manager.IsContextActive().Should().BeFalse();
        _manager.GetContext().Should().BeNull();
        _manager.GetRequestId().Should().BeNull();
        _manager.GetUserId().Should().BeNull();
    }

    [Fact]
    public void IsContextActive_ShouldReflectCurrentState()
    {
        // Initially no context
        _manager.IsContextActive().Should().BeFalse();

        // After creation
        _manager.CreateContext("id-active");
        _manager.IsContextActive().Should().BeTrue();

        // After clear
        _manager.Clear();
        _manager.IsContextActive().Should().BeFalse();
    }

    [Fact]
    public async Task Context_Survives_After_Await_Task_Yield()
    {
        // Arrange
        var manager = new RequestContextManager(Substitute.For<ILogger<RequestContextManager>>());
        var requestId = "req-async-yield";

        // Act - create context and await Task.Yield
        manager.CreateContext(requestId);
        await Task.Yield();

        // Assert - context should still be available
        manager.IsContextActive().Should().BeTrue();
        manager.GetRequestId().Should().Be(requestId);
    }

    [Fact]
    public async Task Context_Isolated_Between_Concurrent_Requests()
    {
        // Arrange
        var manager = new RequestContextManager(Substitute.For<ILogger<RequestContextManager>>());
        var requestId1 = "req-concurrent-1";
        var requestId2 = "req-concurrent-2";
        RequestContext? context1 = null;
        RequestContext? context2 = null;

        // Act - create contexts concurrently and capture them
        var task1 = Task.Run(() => {
            manager.CreateContext(requestId1);
            context1 = manager.GetContext();
        });
        var task2 = Task.Run(() => {
            manager.CreateContext(requestId2);
            context2 = manager.GetContext();
        });

        await Task.WhenAll(task1, task2);

        // Assert - each task should see its own context
        context1.Should().NotBeNull();
        context2.Should().NotBeNull();
        context1?.RequestId.Should().Be(requestId1);
        context2?.RequestId.Should().Be(requestId2);

        // Clear and verify isolation
        manager.Clear();
        manager.IsContextActive().Should().BeFalse();
    }

    [Fact]
    public async Task Context_Flows_Through_Task_Run()
    {
        // Arrange
        var manager = new RequestContextManager(Substitute.For<ILogger<RequestContextManager>>());
        var requestId = "req-background-task";
        RequestContext? capturedContext = null;

        // Act - create context and capture it in background task
        manager.CreateContext(requestId);
        await Task.Run(() => capturedContext = manager.GetContext());

        // Assert - context should be available in background task
        capturedContext.Should().NotBeNull();
        capturedContext?.RequestId.Should().Be(requestId);
        manager.GetRequestId().Should().Be(requestId);
    }

    [Fact]
    public async Task Context_Survives_Multiple_Awaits()
    {
        // Arrange
        var manager = new RequestContextManager(Substitute.For<ILogger<RequestContextManager>>());
        var requestId = "req-multiple-awaits";

        // Act - create context and await multiple times
        manager.CreateContext(requestId);
        await Task.Delay(1);
        await Task.Yield();
        await Task.Delay(1);

        // Assert - context should still be available
        manager.IsContextActive().Should().BeTrue();
        manager.GetRequestId().Should().Be(requestId);
    }

    [Fact]
    public void Clear_Should_Reset_Context_To_Null()
    {
        // Arrange
        var manager = new RequestContextManager(Substitute.For<ILogger<RequestContextManager>>());
        manager.CreateContext("req-clear-test");
        manager.IsContextActive().Should().BeTrue();

        // Act
        manager.Clear();

        // Assert
        manager.IsContextActive().Should().BeFalse();
        manager.GetContext().Should().BeNull();
    }

    [Fact]
    public void Context_Should_Not_Leak_Between_Sync_Contexts()
    {
        // Arrange
        var manager = new RequestContextManager(Substitute.For<ILogger<RequestContextManager>>());

        // Create first context
        manager.CreateContext("req-1");
        manager.GetRequestId().Should().Be("req-1");

        // Clear first context
        manager.Clear();
        manager.IsContextActive().Should().BeFalse();

        // Create second context
        manager.CreateContext("req-2");
        manager.GetRequestId().Should().Be("req-2");
        manager.IsContextActive().Should().BeTrue();
    }
}

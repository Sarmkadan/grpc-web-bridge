#nullable enable
// =============================================================================
// Author: Automated Generation
// =============================================================================

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
}

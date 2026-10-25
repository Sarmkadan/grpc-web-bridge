#nullable enable
// =============================================================================
// Author: Automated Generation
// =============================================================================

using System.Threading.Tasks;
using FluentAssertions;
using GrpcWebBridge.Integration;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace GrpcWebBridge.Tests;

/// <summary>
/// Tests for <see cref="RequestContextMiddleware"/>, verifying that the ambient request context is
/// populated for the duration of a request and cleared once the middleware completes.
/// </summary>
public sealed class RequestContextMiddlewareTests
{
    private readonly RequestContextManager _contextManager;
    private readonly ILogger<RequestContextMiddleware> _mockLogger;

    public RequestContextMiddlewareTests()
    {
        _contextManager = new RequestContextManager(Substitute.For<ILogger<RequestContextManager>>());
        _mockLogger = Substitute.For<ILogger<RequestContextMiddleware>>();
    }

    /// <summary>
    /// Verifies that an incoming X-Request-ID header is used as the request id of the active context,
    /// echoed back on the response, and that the context is cleared after the middleware completes.
    /// </summary>
    [Fact]
    public async Task InvokeAsync_Should_Create_Context_With_RequestId()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Request-ID"] = "test-request-id";
        RequestContext? capturedContext = null;

        var middleware = new RequestContextMiddleware(
            next: (innerHttpContext) =>
            {
                // Capture context during request processing
                capturedContext = _contextManager.GetContext();
                return Task.CompletedTask;
            },
            contextManager: _contextManager,
            logger: _mockLogger);

        // Act
        await middleware.InvokeAsync(context);

        // Assert - context should be active during request processing
        capturedContext.Should().NotBeNull();
        capturedContext?.RequestId.Should().Be("test-request-id");
        _contextManager.IsContextActive().Should().BeFalse(); // Should be cleared after completion
        context.Response.Headers.TryGetValue("X-Request-ID", out var requestIdHeader).Should().BeTrue();
        requestIdHeader.ToString().Should().Be("test-request-id");
    }

    /// <summary>
    /// Verifies that when no X-Request-ID header is present, a non-empty request id is generated for the
    /// active context and written to the response, and that the context is cleared afterwards.
    /// </summary>
    [Fact]
    public async Task InvokeAsync_Should_Create_Context_With_Generated_RequestId()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Headers.Remove("X-Request-ID");
        RequestContext? capturedContext = null;

        var middleware = new RequestContextMiddleware(
            next: (innerHttpContext) =>
            {
                // Capture context during request processing
                capturedContext = _contextManager.GetContext();
                return Task.CompletedTask;
            },
            contextManager: _contextManager,
            logger: _mockLogger);

        // Act
        await middleware.InvokeAsync(context);

        // Assert - context should be active during request processing
        capturedContext.Should().NotBeNull();
        capturedContext?.RequestId.Should().NotBeNullOrEmpty();
        context.Response.Headers.TryGetValue("X-Request-ID", out var requestIdHeader2).Should().BeTrue();
        requestIdHeader2.ToString().Should().NotBeNullOrEmpty();
        _contextManager.IsContextActive().Should().BeFalse(); // Should be cleared after completion
    }

    /// <summary>
    /// Verifies that the user id of the active context is taken from the <c>sub</c> claim of the
    /// authenticated principal, and that the context is cleared after the middleware completes.
    /// </summary>
    [Fact]
    public async Task InvokeAsync_Should_Create_Context_With_UserId_From_Claims()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Request-ID"] = "test-request-id";
        context.User = new System.Security.Claims.ClaimsPrincipal(new[] {
            new System.Security.Claims.ClaimsIdentity(new[] {
                new System.Security.Claims.Claim("sub", "test-user-id")
            }, "test-auth")
        });
        RequestContext? capturedContext = null;

        var middleware = new RequestContextMiddleware(
            next: (innerHttpContext) =>
            {
                // Capture context during request processing
                capturedContext = _contextManager.GetContext();
                return Task.CompletedTask;
            },
            contextManager: _contextManager,
            logger: _mockLogger);

        // Act
        await middleware.InvokeAsync(context);

        // Assert - context should be active during request processing
        capturedContext.Should().NotBeNull();
        capturedContext?.UserId.Should().Be("test-user-id");
        _contextManager.IsContextActive().Should().BeFalse(); // Should be cleared after completion
    }

    /// <summary>
    /// Verifies that once the middleware has finished handling a request, no request context remains
    /// active on the manager.
    /// </summary>
    [Fact]
    public async Task InvokeAsync_Should_Clear_Context_After_Completion()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Request-ID"] = "test-request-id";

        var middleware = new RequestContextMiddleware(
            next: (innerHttpContext) => Task.CompletedTask,
            contextManager: _contextManager,
            logger: _mockLogger);

        // Act
        await middleware.InvokeAsync(context);

        // Assert - context should be cleared after middleware completes
        _contextManager.IsContextActive().Should().BeFalse();
        _contextManager.GetContext().Should().BeNull();
    }

    /// <summary>
    /// Verifies that an exception thrown by the downstream pipeline propagates out of the middleware
    /// unchanged, and that the request context is still cleared.
    /// </summary>
    [Fact]
    public async Task InvokeAsync_Should_Handle_Exceptions_And_Still_Clear_Context()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Request-ID"] = "test-request-id";

        var middleware = new RequestContextMiddleware(
            next: (innerHttpContext) => throw new InvalidOperationException("Test exception"),
            contextManager: _contextManager,
            logger: _mockLogger);

        // Act & Assert - should throw but still clear context
        var act = () => middleware.InvokeAsync(context);
        await act.Should().ThrowAsync<InvalidOperationException>();

        // Assert - context should still be cleared even after exception
        _contextManager.IsContextActive().Should().BeFalse();
    }

    /// <summary>
    /// Verifies that the request context remains accessible after the downstream pipeline awaits
    /// an asynchronous operation, and that it is cleared once the middleware completes.
    /// </summary>
    [Fact]
    public async Task Context_Should_Be_Available_In_Async_Operations()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Request-ID"] = "test-async-context";

        RequestContext? capturedContext = null;

        var middleware = new RequestContextMiddleware(
            next: async (innerHttpContext) =>
            {
                // Simulate async work
                await Task.Delay(10);
                capturedContext = _contextManager.GetContext();
            },
            contextManager: _contextManager,
            logger: _mockLogger);

        // Act
        await middleware.InvokeAsync(context);

        // Assert - context should be available during async operations
        capturedContext.Should().NotBeNull();
        capturedContext?.RequestId.Should().Be("test-async-context");
        _contextManager.IsContextActive().Should().BeFalse(); // Should be cleared after completion
    }

    /// <summary>
    /// Verifies that the request context flows into work started on a thread-pool task via
    /// <c>Task.Run</c> from within the downstream pipeline.
    /// </summary>
    [Fact]
    public async Task Context_Should_Be_Available_In_Background_Tasks()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Request-ID"] = "test-background-task";

        RequestContext? backgroundContext = null;

        var middleware = new RequestContextMiddleware(
            next: async (innerHttpContext) =>
            {
                // Capture context in background task
                await Task.Run(() =>
                {
                    backgroundContext = _contextManager.GetContext();
                });
            },
            contextManager: _contextManager,
            logger: _mockLogger);

        // Act
        await middleware.InvokeAsync(context);

        // Assert - context should be available in background tasks
        backgroundContext.Should().NotBeNull();
        backgroundContext?.RequestId.Should().Be("test-background-task");
    }
}

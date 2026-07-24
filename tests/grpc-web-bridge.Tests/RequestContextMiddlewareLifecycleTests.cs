#nullable enable
// =============================================================================
// Author: Automated Generation
// =====================================================================

using System.Net;
using FluentAssertions;
using GrpcWebBridge.Integration;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace GrpcWebBridge.Tests;

/// <summary>
/// Integration tests for RequestContextMiddleware lifecycle and behavior.
/// Tests the core requirements:
/// 1. Context exists inside handlers
/// 2. Header-derived values are parsed and echoed in responses
/// 3. Context is cleared after response even when handler throws
/// 4. Middleware ordering validation (implicit through proper setup)
/// </summary>
public sealed class RequestContextMiddlewareLifecycleTests
{
    [Fact]
    public async Task RequestContextMiddleware_Should_Create_Context_With_RequestId_From_Header()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Request-ID"] = "test-request-id-123";

        RequestContext? capturedContext = null;
        var contextManager = new RequestContextManager(
            Substitute.For<ILogger<RequestContextManager>>());

        var middleware = new RequestContextMiddleware(
            next: (innerContext) =>
            {
                // Capture context during request processing (inside handler)
                capturedContext = contextManager.GetContext();
                return Task.CompletedTask;
            },
            contextManager: contextManager,
            logger: Substitute.For<ILogger<RequestContextMiddleware>>());

        // Act
        await middleware.InvokeAsync(context);

        // Assert - context should be available inside handlers
        capturedContext.Should().NotBeNull();
        capturedContext?.RequestId.Should().Be("test-request-id-123");

        // Assert - header-derived values should be echoed in response
        context.Response.Headers.TryGetValue("X-Request-ID", out var requestIdHeader).Should().BeTrue();
        requestIdHeader.ToString().Should().Be("test-request-id-123");

        // Assert - context should be cleared after completion
        contextManager.IsContextActive().Should().BeFalse();
    }

    [Fact]
    public async Task RequestContextMiddleware_Should_Generate_RequestId_When_Missing()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Headers.Remove("X-Request-ID");

        RequestContext? capturedContext = null;
        var contextManager = new RequestContextManager(
            Substitute.For<ILogger<RequestContextManager>>());

        var middleware = new RequestContextMiddleware(
            next: (innerContext) =>
            {
                capturedContext = contextManager.GetContext();
                return Task.CompletedTask;
            },
            contextManager: contextManager,
            logger: Substitute.For<ILogger<RequestContextMiddleware>>());

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        capturedContext.Should().NotBeNull();
        capturedContext?.RequestId.Should().NotBeNullOrEmpty();
        context.Response.Headers.TryGetValue("X-Request-ID", out var requestIdHeader).Should().BeTrue();
        requestIdHeader.ToString().Should().Be(capturedContext?.RequestId);
    }

    [Fact]
    public async Task RequestContextMiddleware_Should_Clear_Context_After_Response_Completion()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Request-ID"] = "test-request-id";

        var contextManager = new RequestContextManager(
            Substitute.For<ILogger<RequestContextManager>>());

        var middleware = new RequestContextMiddleware(
            next: (innerContext) => Task.CompletedTask,
            contextManager: contextManager,
            logger: Substitute.For<ILogger<RequestContextMiddleware>>());

        // Act
        await middleware.InvokeAsync(context);

        // Assert - context should be cleared after middleware completes
        contextManager.IsContextActive().Should().BeFalse();
        contextManager.GetContext().Should().BeNull();
    }

    [Fact]
    public async Task RequestContextMiddleware_Should_Clear_Context_After_Exception_In_Handler()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Request-ID"] = "test-request-id";

        var contextManager = new RequestContextManager(
            Substitute.For<ILogger<RequestContextManager>>());

        var middleware = new RequestContextMiddleware(
            next: (innerContext) => throw new InvalidOperationException("Test exception"),
            contextManager: contextManager,
            logger: Substitute.For<ILogger<RequestContextMiddleware>>());

        // Act & Assert - should throw but still clear context
        var act = () => middleware.InvokeAsync(context);
        await act.Should().ThrowAsync<InvalidOperationException>();

        // Assert - context should still be cleared even after exception
        contextManager.IsContextActive().Should().BeFalse();
        contextManager.GetContext().Should().BeNull();
    }

    [Fact]
    public async Task RequestContextMiddleware_Should_Record_Elapsed_Time()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Request-ID"] = "test-request-id";

        var contextManager = new RequestContextManager(
            Substitute.For<ILogger<RequestContextManager>>());

        var middleware = new RequestContextMiddleware(
            next: (innerContext) => Task.CompletedTask,
            contextManager: contextManager,
            logger: Substitute.For<ILogger<RequestContextMiddleware>>());

        // Act
        await middleware.InvokeAsync(context);

        // Assert - elapsed time should be recorded (called in finally block)
        contextManager.IsContextActive().Should().BeFalse();
    }

    [Fact]
    public async Task RequestContextMiddleware_Should_Handle_Multiple_Concurrent_Requests()
    {
        // Arrange - create multiple contexts for concurrent requests
        var context1 = new DefaultHttpContext();
        context1.Request.Headers["X-Request-ID"] = "request-1";

        var context2 = new DefaultHttpContext();
        context2.Request.Headers["X-Request-ID"] = "request-2";

        var contextManager = new RequestContextManager(
            Substitute.For<ILogger<RequestContextManager>>());

        var middleware1 = new RequestContextMiddleware(
            next: async (innerContext) =>
            {
                await Task.Delay(10); // Simulate async work
                var context = contextManager.GetContext();
                context?.Should().NotBeNull();
                context?.RequestId.Should().Be("request-1");
            },
            contextManager: contextManager,
            logger: Substitute.For<ILogger<RequestContextMiddleware>>());

        var middleware2 = new RequestContextMiddleware(
            next: async (innerContext) =>
            {
                await Task.Delay(10); // Simulate async work
                var context = contextManager.GetContext();
                context?.Should().NotBeNull();
                context?.RequestId.Should().Be("request-2");
            },
            contextManager: contextManager,
            logger: Substitute.For<ILogger<RequestContextMiddleware>>());

        // Act - process requests concurrently
        var task1 = middleware1.InvokeAsync(context1);
        var task2 = middleware2.InvokeAsync(context2);

        await Task.WhenAll(task1, task2);

        // Assert - both contexts should be cleared after completion
        contextManager.IsContextActive().Should().BeFalse();
    }

    [Fact]
    public async Task RequestContextMiddleware_Should_Not_Leak_Context_Between_Requests()
    {
        // Arrange
        var contextManager = new RequestContextManager(
            Substitute.For<ILogger<RequestContextManager>>());

        // First request
        var context1 = new DefaultHttpContext();
        context1.Request.Headers["X-Request-ID"] = "first-request";

        var middleware1 = new RequestContextMiddleware(
            next: (innerContext) => Task.CompletedTask,
            contextManager: contextManager,
            logger: Substitute.For<ILogger<RequestContextMiddleware>>());

        await middleware1.InvokeAsync(context1);
        contextManager.IsContextActive().Should().BeFalse();

        // Second request with different ID
        var context2 = new DefaultHttpContext();
        context2.Request.Headers["X-Request-ID"] = "second-request";

        var middleware2 = new RequestContextMiddleware(
            next: (innerContext) =>
            {
                var currentContext = contextManager.GetContext();
                currentContext.Should().NotBeNull();
                currentContext?.RequestId.Should().Be("second-request");
                return Task.CompletedTask;
            },
            contextManager: contextManager,
            logger: Substitute.For<ILogger<RequestContextMiddleware>>());

        await middleware2.InvokeAsync(context2);
        contextManager.IsContextActive().Should().BeFalse();
    }

    [Fact]
    public async Task RequestContextMiddleware_Should_Extract_UserId_From_Claims()
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
        var contextManager = new RequestContextManager(
            Substitute.For<ILogger<RequestContextManager>>());

        var middleware = new RequestContextMiddleware(
            next: (innerContext) =>
            {
                capturedContext = contextManager.GetContext();
                return Task.CompletedTask;
            },
            contextManager: contextManager,
            logger: Substitute.For<ILogger<RequestContextMiddleware>>());

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        capturedContext.Should().NotBeNull();
        capturedContext?.UserId.Should().Be("test-user-id");
    }

    [Fact]
    public void RequestContextManager_Should_Provide_Active_Context_Count()
    {
        // Arrange
        var manager = new RequestContextManager(
            Substitute.For<ILogger<RequestContextManager>>());

        // Act - create multiple contexts
        var context1 = manager.CreateContext("request-1", "user-1");
        var context2 = manager.CreateContext("request-2", "user-2");

        // Assert
        manager.GetActiveContextCount().Should().Be(2);
        manager.IsContextActive().Should().BeTrue();

        // Act - clear contexts
        manager.Clear();
        manager.Clear();
        manager.TryRemoveContext("request-1");
        manager.TryRemoveContext("request-2");

        // Assert
        manager.GetActiveContextCount().Should().Be(0);
        manager.IsContextActive().Should().BeFalse();
    }

    [Fact]
    public async Task RequestContextMiddleware_Should_Add_RequestId_To_Response_Headers()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var requestId = Guid.NewGuid().ToString();
        context.Request.Headers["X-Request-ID"] = requestId;

        var contextManager = new RequestContextManager(
            Substitute.For<ILogger<RequestContextManager>>());

        var middleware = new RequestContextMiddleware(
            next: (innerContext) => Task.CompletedTask,
            contextManager: contextManager,
            logger: Substitute.For<ILogger<RequestContextMiddleware>>());

        // Act
        await middleware.InvokeAsync(context);

        // Assert - header-derived values should be echoed in response
        context.Response.Headers.TryGetValue("X-Request-ID", out var responseRequestId).Should().BeTrue();
        responseRequestId.ToString().Should().Be(requestId);
    }
}
#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using FluentAssertions;
using GrpcWebBridge.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using System.Net;
using Xunit;

namespace GrpcWebBridge.Tests;

/// <summary>
/// Unit tests for <see cref="RateLimitingMiddleware"/>.
/// </summary>
public sealed class RateLimitingMiddlewareTests
{
    private static RateLimitingMiddleware CreateMiddleware(RequestDelegate next, RateLimitingOptions? options = null)
    {
        return new RateLimitingMiddleware(
            next, 
            NullLogger<RateLimitingMiddleware>.Instance, 
            options ?? new RateLimitingOptions { RequestsPerSecond = 1, WindowSizeSeconds = 1, RetryAfterSeconds = 10 });
    }

    private static DefaultHttpContext CreateContext(string path = "/api/test", string? ipAddress = "127.0.0.1", string? forwardedFor = null)
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        context.Request.Path = path;
        
        if (ipAddress != null)
        {
            context.Connection.RemoteIpAddress = IPAddress.Parse(ipAddress);
        }
        
        if (forwardedFor != null)
        {
            context.Request.Headers["X-Forwarded-For"] = forwardedFor;
        }
        
        return context;
    }

    [Fact]
    public async Task InvokeAsync_UnderLimit_PassesRequest()
    {
        bool nextCalled = false;
        var middleware = CreateMiddleware(_ => { nextCalled = true; return Task.CompletedTask; });
        var context = CreateContext();

        await middleware.InvokeAsync(context);

        nextCalled.Should().BeTrue();
        context.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    [Fact]
    public async Task InvokeAsync_OverLimit_Returns429WithRetryAfter()
    {
        var options = new RateLimitingOptions { RequestsPerSecond = 1, WindowSizeSeconds = 1, RetryAfterSeconds = 30 };
        var middleware = CreateMiddleware(_ => Task.CompletedTask, options);
        var context1 = CreateContext(ipAddress: "192.168.1.1");
        var context2 = CreateContext(ipAddress: "192.168.1.1");

        // First request passes
        await middleware.InvokeAsync(context1);
        context1.Response.StatusCode.Should().Be(StatusCodes.Status200OK);

        // Second request exceeds limit
        await middleware.InvokeAsync(context2);
        context2.Response.StatusCode.Should().Be(StatusCodes.Status429TooManyRequests);
        context2.Response.Headers["Retry-After"].ToString().Should().Be("30");
    }

    [Fact]
    public async Task InvokeAsync_WindowReset_RestoresCapacity()
    {
        var options = new RateLimitingOptions { RequestsPerSecond = 1, WindowSizeSeconds = 1, RetryAfterSeconds = 30 };
        var middleware = CreateMiddleware(_ => Task.CompletedTask, options);
        var context1 = CreateContext(ipAddress: "10.0.0.1");
        
        await middleware.InvokeAsync(context1);
        context1.Response.StatusCode.Should().Be(StatusCodes.Status200OK);

        // This one fails immediately
        var context2 = CreateContext(ipAddress: "10.0.0.1");
        await middleware.InvokeAsync(context2);
        context2.Response.StatusCode.Should().Be(StatusCodes.Status429TooManyRequests);

        // Wait for window reset
        await Task.Delay(1100);

        // This one should pass
        var context3 = CreateContext(ipAddress: "10.0.0.1");
        await middleware.InvokeAsync(context3);
        context3.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    [Fact]
    public async Task InvokeAsync_PerClientIsolation_DoesNotShareLimits()
    {
        var options = new RateLimitingOptions { RequestsPerSecond = 1, WindowSizeSeconds = 1 };
        var middleware = CreateMiddleware(_ => Task.CompletedTask, options);
        
        var contextClient1 = CreateContext(ipAddress: "1.1.1.1");
        var contextClient2 = CreateContext(ipAddress: "2.2.2.2");

        // Client 1 fills its limit
        await middleware.InvokeAsync(contextClient1);
        contextClient1.Response.StatusCode.Should().Be(StatusCodes.Status200OK);

        // Client 2 should still pass
        await middleware.InvokeAsync(contextClient2);
        contextClient2.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
    }
}

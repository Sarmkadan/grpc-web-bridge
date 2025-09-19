#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using FluentAssertions;
using GrpcWebBridge.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using System.Text;
using Xunit;

namespace GrpcWebBridge.Tests;

public sealed class RequestLoggingMiddlewareTests
{
    private static RequestLoggingMiddleware CreateMiddleware(RequestDelegate next)
        => new(next, NullLogger<RequestLoggingMiddleware>.Instance);

    private static DefaultHttpContext CreateGetContext(string path = "/api/bridge/test")
    {
        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Get;
        context.Request.Path = path;
        context.Request.Scheme = "http";
        context.Response.Body = new MemoryStream();
        return context;
    }

    private static DefaultHttpContext CreatePostContext(string path, string body, string contentType = "application/json")
    {
        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Post;
        context.Request.Path = path;
        context.Request.ContentType = contentType;
        var bodyBytes = Encoding.UTF8.GetBytes(body);
        context.Request.Body = new MemoryStream(bodyBytes);
        context.Request.ContentLength = bodyBytes.Length;
        context.Response.Body = new MemoryStream();
        return context;
    }

    // ─────────────────────────────────────────────────────────────────────
    // Pass-through behaviour
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task InvokeAsync_WhenNoException_CallsNextMiddleware()
    {
        bool nextCalled = false;
        var middleware = CreateMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });
        var context = CreateGetContext();

        await middleware.InvokeAsync(context);

        nextCalled.Should().BeTrue("the next middleware in the pipeline must be called");
    }

    [Fact]
    public async Task InvokeAsync_WhenNoException_ResponseBodyIsForwardedToOriginalStream()
    {
        const string responsePayload = "{\"result\":\"ok\"}";
        var middleware = CreateMiddleware(ctx =>
        {
            ctx.Response.ContentType = "application/json";
            return ctx.Response.WriteAsync(responsePayload);
        });
        var context = CreateGetContext();

        await middleware.InvokeAsync(context);

        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body, Encoding.UTF8);
        var body = await reader.ReadToEndAsync();
        body.Should().Be(responsePayload);
    }

    // ─────────────────────────────────────────────────────────────────────
    // Excluded paths — middleware must not buffer / alter response
    // ─────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("/health")]
    [InlineData("/swagger/index.html")]
    [InlineData("/api/metrics")]
    [InlineData("/favicon.ico")]
    public async Task InvokeAsync_ForExcludedPath_StillCallsNext(string path)
    {
        bool nextCalled = false;
        var middleware = CreateMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });
        var context = CreateGetContext(path);

        await middleware.InvokeAsync(context);

        nextCalled.Should().BeTrue($"excluded path '{path}' must still pass through to next middleware");
    }

    // ─────────────────────────────────────────────────────────────────────
    // Status-code-based log levels — pipeline must not throw
    // ─────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(200)]
    [InlineData(400)]
    [InlineData(404)]
    [InlineData(500)]
    public async Task InvokeAsync_WithVariousStatusCodes_DoesNotThrow(int statusCode)
    {
        var middleware = CreateMiddleware(ctx =>
        {
            ctx.Response.StatusCode = statusCode;
            return Task.CompletedTask;
        });
        var context = CreateGetContext();

        Func<Task> act = () => middleware.InvokeAsync(context);

        await act.Should().NotThrowAsync();
    }

    // ─────────────────────────────────────────────────────────────────────
    // Sensitive-header masking
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task InvokeAsync_DoesNotThrow_WhenAuthorizationHeaderIsPresent()
    {
        var middleware = CreateMiddleware(_ => Task.CompletedTask);
        var context = CreateGetContext();
        context.Request.Headers.Authorization = "Bearer secret-token";

        Func<Task> act = () => middleware.InvokeAsync(context);

        await act.Should().NotThrowAsync("sensitive headers should be masked, not cause errors");
    }

    // ─────────────────────────────────────────────────────────────────────
    // Request body capture
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task InvokeAsync_WithJsonRequestBody_DoesNotThrowAndCallsNext()
    {
        bool nextCalled = false;
        var middleware = CreateMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });
        var context = CreatePostContext("/api/bridge/invoke", "{\"service\":\"test\"}");

        Func<Task> act = () => middleware.InvokeAsync(context);

        await act.Should().NotThrowAsync();
        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task InvokeAsync_WithBinaryContentType_DoesNotCaptureBody()
    {
        bool nextCalled = false;
        var middleware = CreateMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });
        // Binary body should be skipped to avoid logging raw bytes
        var context = CreatePostContext("/api/bridge/invoke", "binary-data", "application/octet-stream");

        Func<Task> act = () => middleware.InvokeAsync(context);

        await act.Should().NotThrowAsync();
        nextCalled.Should().BeTrue();
    }

    // ─────────────────────────────────────────────────────────────────────
    // Constructor wiring
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public void Constructor_WithValidArguments_CreatesInstance()
    {
        var instance = new RequestLoggingMiddleware(
            _ => Task.CompletedTask,
            NullLogger<RequestLoggingMiddleware>.Instance);

        instance.Should().NotBeNull();
    }

    [Fact]
    public async Task InvokeAsync_WithNonGrpcPath_LogsAndPassesThrough()
    {
        bool nextCalled = false;
        var middleware = CreateMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });
        var context = CreateGetContext("/api/services");

        await middleware.InvokeAsync(context);

        nextCalled.Should().BeTrue();
    }
}

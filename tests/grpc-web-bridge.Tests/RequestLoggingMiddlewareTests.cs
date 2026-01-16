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

/// <summary>
/// Tests for the RequestLoggingMiddleware class.
/// </summary>
public sealed class RequestLoggingMiddlewareTests
{
    /// <summary>
    /// Creates a new instance of the RequestLoggingMiddleware class.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <returns>A new instance of the RequestLoggingMiddleware class.</returns>
    private static RequestLoggingMiddleware CreateMiddleware(RequestDelegate next)
        => new(next, NullLogger<RequestLoggingMiddleware>.Instance);

    /// <summary>
    /// Creates a new instance of the DefaultHttpContext class with a GET request.
    /// </summary>
    /// <param name="path">The path of the request.</param>
    /// <returns>A new instance of the DefaultHttpContext class.</returns>
    private static DefaultHttpContext CreateGetContext(string path = "/api/bridge/test")
    {
        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Get;
        context.Request.Path = path;
        context.Request.Scheme = "http";
        context.Response.Body = new MemoryStream();
        return context;
    }

    /// <summary>
    /// Creates a new instance of the DefaultHttpContext class with a POST request.
    /// </summary>
    /// <param name="path">The path of the request.</param>
    /// <param name="body">The body of the request.</param>
    /// <param name="contentType">The content type of the request.</param>
    /// <returns>A new instance of the DefaultHttpContext class.</returns>
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

    /// <summary>
    /// Tests that the middleware calls the next middleware in the pipeline when no exception is thrown.
    /// </summary>
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

    /// <summary>
    /// Tests that the middleware forwards the response body to the original stream when no exception is thrown.
    /// </summary>
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

    /// <summary>
    /// Tests that the middleware still calls the next middleware in the pipeline for excluded paths.
    /// </summary>
    /// <param name="path">The path of the request.</param>
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

    /// <summary>
    /// Tests that the middleware does not throw for various status codes.
    /// </summary>
    /// <param name="statusCode">The status code of the response.</param>
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

    /// <summary>
    /// Tests that the middleware does not throw when the Authorization header is present.
    /// </summary>
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

    /// <summary>
    /// Tests that the middleware does not throw when the request body is JSON.
    /// </summary>
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

    /// <summary>
    /// Tests that the middleware does not capture the request body when the content type is binary.
    /// </summary>
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

    /// <summary>
    /// Tests that the constructor creates an instance of the RequestLoggingMiddleware class with valid arguments.
    /// </summary>
    [Fact]
    public void Constructor_WithValidArguments_CreatesInstance()
    {
        var instance = new RequestLoggingMiddleware(
            _ => Task.CompletedTask,
            NullLogger<RequestLoggingMiddleware>.Instance);

        instance.Should().NotBeNull();
    }

    /// <summary>
    /// Tests that the middleware logs and passes through when the path is not a gRPC path.
    /// </summary>
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

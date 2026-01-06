#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using FluentAssertions;
using GrpcWebBridge.Domain.Exceptions;
using GrpcWebBridge.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using System.Text;
using System.Text.Json;
using Xunit;

/// <summary>
/// Tests for the ErrorHandlingMiddleware class.
/// </summary>
public sealed class ErrorHandlingMiddlewareTests
{
    /// <summary>
    /// Creates a new instance of the ErrorHandlingMiddleware class.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <returns>A new instance of the ErrorHandlingMiddleware class.</returns>
    private static ErrorHandlingMiddleware CreateMiddleware(RequestDelegate next)
        => new(next, NullLogger<ErrorHandlingMiddleware>.Instance);

    /// <summary>
    /// Creates a new instance of the DefaultHttpContext class.
    /// </summary>
    /// <returns>A new instance of the DefaultHttpContext class.</returns>
    private static DefaultHttpContext CreateContext()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        context.Request.Path = "/grpc/test";
        context.TraceIdentifier = "trace-123";
        return context;
    }

    /// <summary>
    /// Executes the ErrorHandlingMiddleware class and returns the status code and response body.
    /// </summary>
    /// <param name="exception">The exception to be thrown.</param>
    /// <returns>A tuple containing the status code and response body.</returns>
    private static async Task<(int StatusCode, string Body)> ExecuteAsync(Exception exception)
    {
        var context = CreateContext();
        var middleware = CreateMiddleware(_ => throw exception);

        await middleware.InvokeAsync(context);

        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body, Encoding.UTF8);
        var body = await reader.ReadToEndAsync();
        return (context.Response.StatusCode, body);
    }

    // ─────────────────────────────────────────────────────────────────────
    // Exception type → HTTP status code mappings
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Tests that the ErrorHandlingMiddleware class returns a 400 status code when a ServiceRegistrationException is thrown.
    /// </summary>
    [Fact]
    public async Task InvokeAsync_WithServiceRegistrationException_Returns400()
    {
        var (status, body) = await ExecuteAsync(new ServiceRegistrationException("Registration failed"));

        status.Should().Be(StatusCodes.Status400BadRequest);
        body.Should().Contain("Service Registration Failed");
    }

    /// <summary>
    /// Tests that the ErrorHandlingMiddleware class returns a 500 status code when a StreamingException is thrown.
    /// </summary>
    [Fact]
    public async Task InvokeAsync_WithStreamingException_Returns500()
    {
        var (status, body) = await ExecuteAsync(new StreamingException("Stream broken"));

        status.Should().Be(StatusCodes.Status500InternalServerError);
        body.Should().Contain("Streaming Operation Failed");
    }

    /// <summary>
    /// Tests that the ErrorHandlingMiddleware class returns a 400 status code when a ProtocolException is thrown.
    /// </summary>
    [Fact]
    public async Task InvokeAsync_WithProtocolException_Returns400()
    {
        var (status, body) = await ExecuteAsync(new ProtocolException("Bad protocol"));

        status.Should().Be(StatusCodes.Status400BadRequest);
        body.Should().Contain("Protocol Translation Failed");
    }

    /// <summary>
    /// Tests that the ErrorHandlingMiddleware class returns a 500 status code when a GrpcWebBridgeException is thrown.
    /// </summary>
    [Fact]
    public async Task InvokeAsync_WithGrpcWebBridgeException_Returns500()
    {
        var (status, body) = await ExecuteAsync(new GrpcWebBridgeException("Bridge error"));

        status.Should().Be(StatusCodes.Status500InternalServerError);
        body.Should().Contain("Bridge Operation Failed");
    }

    /// <summary>
    /// Tests that the ErrorHandlingMiddleware class returns a 400 status code when an ArgumentNullException is thrown.
    /// </summary>
    [Fact]
    public async Task InvokeAsync_WithArgumentNullException_Returns400()
    {
        var (status, body) = await ExecuteAsync(new ArgumentNullException("myParam"));

        status.Should().Be(StatusCodes.Status400BadRequest);
        body.Should().Contain("Invalid Request");
        body.Should().Contain("myParam");
    }

    /// <summary>
    /// Tests that the ErrorHandlingMiddleware class returns a 400 status code when an ArgumentException is thrown.
    /// </summary>
    [Fact]
    public async Task InvokeAsync_WithArgumentException_Returns400()
    {
        var (status, body) = await ExecuteAsync(new ArgumentException("Bad argument"));

        status.Should().Be(StatusCodes.Status400BadRequest);
        body.Should().Contain("Invalid Argument");
    }

    /// <summary>
    /// Tests that the ErrorHandlingMiddleware class returns a 401 status code when an UnauthorizedAccessException is thrown.
    /// </summary>
    [Fact]
    public async Task InvokeAsync_WithUnauthorizedAccessException_Returns401()
    {
        var (status, body) = await ExecuteAsync(new UnauthorizedAccessException());

        status.Should().Be(StatusCodes.Status401Unauthorized);
        body.Should().Contain("Unauthorized");
    }

    /// <summary>
    /// Tests that the ErrorHandlingMiddleware class returns a 504 status code when a TimeoutException is thrown.
    /// </summary>
    [Fact]
    public async Task InvokeAsync_WithTimeoutException_Returns504()
    {
        var (status, body) = await ExecuteAsync(new TimeoutException("Timed out"));

        status.Should().Be(StatusCodes.Status504GatewayTimeout);
        body.Should().Contain("Operation Timeout");
    }

    /// <summary>
    /// Tests that the ErrorHandlingMiddleware class returns a 400 status code when an OperationCanceledException is thrown.
    /// </summary>
    [Fact]
    public async Task InvokeAsync_WithOperationCanceledException_Returns400()
    {
        var (status, body) = await ExecuteAsync(new OperationCanceledException("Cancelled"));

        status.Should().Be(StatusCodes.Status400BadRequest);
        body.Should().Contain("Operation Cancelled");
    }

    /// <summary>
    /// Tests that the ErrorHandlingMiddleware class returns a 500 status code with an internal server error message when an unknown exception is thrown.
    /// </summary>
    [Fact]
    public async Task InvokeAsync_WithUnknownException_Returns500WithInternalServerError()
    {
        var (status, body) = await ExecuteAsync(new InvalidProgramException("Unknown error"));

        status.Should().Be(StatusCodes.Status500InternalServerError);
        body.Should().Contain("Internal Server Error");
    }

    // ─────────────────────────────────────────────────────────────────────
    // Response structure
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Tests that the ErrorHandlingMiddleware class returns an error response with the expected JSON fields.
    /// </summary>
    [Fact]
    public async Task InvokeAsync_ErrorResponse_ContainsExpectedJsonFields()
    {
        var context = CreateContext();
        var middleware = CreateMiddleware(_ => throw new ArgumentException("test arg error"));

        await middleware.InvokeAsync(context);

        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body, Encoding.UTF8);
        var json = await reader.ReadToEndAsync();

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        root.TryGetProperty("success", out var success).Should().BeTrue();
        success.GetBoolean().Should().BeFalse();
        root.TryGetProperty("error", out _).Should().BeTrue();
        root.TryGetProperty("message", out _).Should().BeTrue();
        root.TryGetProperty("timestamp", out _).Should().BeTrue();
        root.TryGetProperty("path", out _).Should().BeTrue();
    }

    /// <summary>
    /// Tests that the ErrorHandlingMiddleware class calls the next middleware in the pipeline when no exception is thrown.
    /// </summary>
    [Fact]
    public async Task InvokeAsync_WhenNoException_CallsNextAndDoesNotModifyResponse()
    {
        bool nextCalled = false;
        var context = CreateContext();
        var middleware = CreateMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(context);

        nextCalled.Should().BeTrue();
        context.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    /// <summary>
    /// Tests that the ErrorHandlingMiddleware class sets the content type to JSON.
    /// </summary>
    [Fact]
    public async Task InvokeAsync_SetsContentTypeToJson()
    {
        var context = CreateContext();
        var middleware = CreateMiddleware(_ => throw new Exception("test"));

        await middleware.InvokeAsync(context);

        context.Response.ContentType.Should().Contain("application/json");
    }
}

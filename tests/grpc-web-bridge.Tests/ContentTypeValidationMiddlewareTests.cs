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
using System.Text.Json;
using Xunit;

/// <summary>
/// Tests for the <see cref="ContentTypeValidationMiddleware"/>.
/// </summary>
public sealed class ContentTypeValidationMiddlewareTests
{
    /// <summary>
    /// Creates a new instance of the <see cref="ContentTypeValidationMiddleware"/>
    /// with the specified <paramref name="next"/> delegate.
    /// </summary>
    /// <param name="next">The next middleware delegate.</param>
    /// <returns>A new instance of the <see cref="ContentTypeValidationMiddleware"/>.</returns>
    private static ContentTypeValidationMiddleware CreateMiddleware(RequestDelegate next)
        => new(next, NullLogger<ContentTypeValidationMiddleware>.Instance);

    /// <summary>
    /// Creates a new <see cref="DefaultHttpContext"/> instance with the specified
    /// <paramref name="path"/> and <paramref name="contentType"/>.
    /// </summary>
    /// <param name="path">The request path.</param>
    /// <param name="contentType">The request content type.</param>
    /// <returns>A new <see cref="DefaultHttpContext"/> instance.</returns>
    private static DefaultHttpContext CreatePostContext(string path, string? contentType)
    {
        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Post;
        context.Request.Path = path;
        if (contentType is not null)
            context.Request.ContentType = contentType;
        context.Response.Body = new MemoryStream();
        return context;
    }

    // ─────────────────────────────────────────────────────────────────────
    // Allowed gRPC-Web content types — should pass through
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that the <see cref="ContentTypeValidationMiddleware"/> allows
    /// gRPC-Web content types to pass through.
    /// </summary>
    /// <param name="contentType">The content type to test.</param>
    [Theory]
    [InlineData("application/grpc-web")]
    [InlineData("application/grpc-web+proto")]
    [InlineData("application/grpc-web-text")]
    [InlineData("application/grpc-web-text+proto")]
    [InlineData("application/grpc+proto")]
    [InlineData("application/grpc")]
    [InlineData("application/grpc-web; charset=utf-8")]
    public async Task InvokeAsync_WithValidGrpcContentType_CallsNextMiddleware(string contentType)
    {
        bool nextCalled = false;
        var middleware = CreateMiddleware(_ => { nextCalled = true; return Task.CompletedTask; });
        var context = CreatePostContext("/grpc/TestService/TestMethod", contentType);

        await middleware.InvokeAsync(context);

        nextCalled.Should().BeTrue("valid gRPC-Web content type should pass validation");
        context.Response.StatusCode.Should().NotBe(StatusCodes.Status415UnsupportedMediaType);
    }

    // ─────────────────────────────────────────────────────────────────────
    // Invalid content types — should be rejected
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that the <see cref="ContentTypeValidationMiddleware"/> rejects
    /// invalid content types.
    /// </summary>
    /// <param name="contentType">The content type to test.</param>
    [Theory]
    [InlineData("application/json")]
    [InlineData("text/plain")]
    [InlineData("application/xml")]
    [InlineData("multipart/form-data")]
    public async Task InvokeAsync_WithInvalidContentType_Returns415(string contentType)
    {
        bool nextCalled = false;
        var middleware = CreateMiddleware(_ => { nextCalled = true; return Task.CompletedTask; });
        var context = CreatePostContext("/grpc/TestService/TestMethod", contentType);

        await middleware.InvokeAsync(context);

        nextCalled.Should().BeFalse();
        context.Response.StatusCode.Should().Be(StatusCodes.Status415UnsupportedMediaType);
    }

    /// <summary>
    /// Verifies that the <see cref="ContentTypeValidationMiddleware"/> returns
    /// a 415 status code when the content type is missing.
    /// </summary>
    [Fact]
    public async Task InvokeAsync_WithMissingContentType_Returns415()
    {
        bool nextCalled = false;
        var middleware = CreateMiddleware(_ => { nextCalled = true; return Task.CompletedTask; });
        var context = CreatePostContext("/grpc/SomeService/Method", contentType: null);

        await middleware.InvokeAsync(context);

        nextCalled.Should().BeFalse();
        context.Response.StatusCode.Should().Be(StatusCodes.Status415UnsupportedMediaType);
    }

    // ─────────────────────────────────────────────────────────────────────
    // Excluded paths — should bypass validation entirely
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that the <see cref="ContentTypeValidationMiddleware"/> bypasses
    /// validation for excluded paths.
    /// </summary>
    /// <param name="path">The path to test.</param>
    [Theory]
    [InlineData("/api/services")]
    [InlineData("/swagger/index.html")]
    [InlineData("/openapi/v1.json")]
    [InlineData("/health")]
    [InlineData("/metrics")]
    [InlineData("/_internal")]
    public async Task InvokeAsync_WithExcludedPath_BypassesValidation(string path)
    {
        bool nextCalled = false;
        var middleware = CreateMiddleware(_ => { nextCalled = true; return Task.CompletedTask; });
        var context = CreatePostContext(path, contentType: "application/json");

        await middleware.InvokeAsync(context);

        nextCalled.Should().BeTrue("excluded paths skip content-type validation");
    }

    // ─────────────────────────────────────────────────────────────────────
    // Non-POST methods — should always pass through
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that the <see cref="ContentTypeValidationMiddleware"/> allows
    /// non-POST methods to pass through.
    /// </summary>
    /// <param name="method">The HTTP method to test.</param>
    [Theory]
    [InlineData("GET")]
    [InlineData("PUT")]
    [InlineData("DELETE")]
    [InlineData("OPTIONS")]
    public async Task InvokeAsync_WithNonPostMethod_BypassesValidation(string method)
    {
        bool nextCalled = false;
        var middleware = CreateMiddleware(_ => { nextCalled = true; return Task.CompletedTask; });
        var context = new DefaultHttpContext();
        context.Request.Method = method;
        context.Request.Path = "/grpc/SomeService/Method";
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        nextCalled.Should().BeTrue($"{method} requests are not subject to content-type validation");
    }

    // ─────────────────────────────────────────────────────────────────────
    // Error response body
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that the <see cref="ContentTypeValidationMiddleware"/> writes
    /// a JSON error response body when the content type is invalid.
    /// </summary>
    [Fact]
    public async Task InvokeAsync_WithInvalidContentType_WritesJsonErrorBody()
    {
        var middleware = CreateMiddleware(_ => Task.CompletedTask);
        var context = CreatePostContext("/grpc/SomeService/Method", "text/plain");

        await middleware.InvokeAsync(context);

        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body, Encoding.UTF8);
        var body = await reader.ReadToEndAsync();

        body.Should().Contain("Unsupported Media Type");
        context.Response.ContentType.Should().Contain("application/json");
    }
}

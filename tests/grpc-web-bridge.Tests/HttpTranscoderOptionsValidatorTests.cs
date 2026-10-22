#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Text.Json;
using FluentAssertions;
using GrpcWebBridge.Configuration;
using Xunit;

namespace GrpcWebBridge.Tests;

/// <summary>
/// Test suite for <see cref="HttpTranscoderOptionsValidator"/>.
/// </summary>
public sealed class HttpTranscoderOptionsValidatorTests
{
    private readonly HttpTranscoderOptionsValidator _validator = new();

    /// <summary>
    /// Ensures that the default options pass validation.
    /// </summary>
    [Fact]
    public void Validate_WithDefaultOptions_Succeeds()
    {
        // Act
        var result = _validator.Validate(null, new HttpTranscoderOptions());

        // Assert
        result.Succeeded.Should().BeTrue();
    }

    /// <summary>
    /// Ensures that a well-formed route prefix is accepted.
    /// </summary>
    [Theory]
    [InlineData("/api")]
    [InlineData("/api/v1")]
    public void Validate_WithValidRoutePrefix_Succeeds(string prefix)
    {
        // Arrange
        var options = new HttpTranscoderOptions { RoutePrefix = prefix };

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        result.Succeeded.Should().BeTrue();
    }

    /// <summary>
    /// Ensures that a route prefix must start with a slash.
    /// </summary>
    [Theory]
    [InlineData("api")]
    [InlineData("")]
    public void Validate_WithRoutePrefixNotStartingWithSlash_Fails(string prefix)
    {
        // Arrange
        var options = new HttpTranscoderOptions { RoutePrefix = prefix };

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        result.Failed.Should().BeTrue();
        result.Failures.Should().ContainSingle()
            .Which.Should().Contain("HttpTranscoderOptions.RoutePrefix must start with '/'");
    }

    /// <summary>
    /// Ensures that a route prefix must not end with a slash, including the bare root slash.
    /// </summary>
    [Theory]
    [InlineData("/api/")]
    [InlineData("/")]
    public void Validate_WithRoutePrefixEndingWithSlash_Fails(string prefix)
    {
        // Arrange
        var options = new HttpTranscoderOptions { RoutePrefix = prefix };

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        result.Failed.Should().BeTrue();
        result.Failures.Should().ContainSingle()
            .Which.Should().Contain("HttpTranscoderOptions.RoutePrefix must not end with '/'");
    }

    /// <summary>
    /// Ensures that null JSON serializer settings are rejected.
    /// </summary>
    [Fact]
    public void Validate_WithNullJsonSerializerOptions_Fails()
    {
        // Arrange
        var options = new HttpTranscoderOptions { JsonSerializerOptions = null };

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        result.Failed.Should().BeTrue();
        result.Failures.Should().ContainSingle()
            .Which.Should().Contain("HttpTranscoderOptions.JsonSerializerOptions must not be null");
    }

    /// <summary>
    /// Ensures that non-null JSON serializer settings are accepted.
    /// </summary>
    [Fact]
    public void Validate_WithJsonSerializerOptions_Succeeds()
    {
        // Arrange
        var options = new HttpTranscoderOptions { JsonSerializerOptions = new JsonSerializerOptions() };

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        result.Succeeded.Should().BeTrue();
    }

    /// <summary>
    /// Ensures that a non-positive max request body size is rejected.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_WithNonPositiveMaxRequestBodySize_Fails(long size)
    {
        // Arrange
        var options = new HttpTranscoderOptions { MaxRequestBodySize = size };

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        result.Failed.Should().BeTrue();
        result.Failures.Should().ContainSingle()
            .Which.Should().Contain("HttpTranscoderOptions.MaxRequestBodySize must be greater than 0");
    }

    /// <summary>
    /// Ensures that a null route list is rejected.
    /// </summary>
    [Fact]
    public void Validate_WithNullRoutes_Fails()
    {
        // Arrange
        var options = new HttpTranscoderOptions { Routes = null };

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        result.Failed.Should().BeTrue();
        result.Failures.Should().ContainSingle()
            .Which.Should().Contain("HttpTranscoderOptions.Routes must not be null");
    }

    /// <summary>
    /// Ensures that a valid route template, including a pattern variable, is accepted.
    /// </summary>
    [Theory]
    [InlineData("/v1/items/{id}", "id")]
    [InlineData("/v1/items/{id=*}", "id")]
    [InlineData("/v1/{book.id}", "book")]
    [InlineData("/v1/shelves/{name=books/*}", "name")]
    public void Validate_WithValidRouteTemplate_Succeeds(string template, string field)
    {
        // Arrange
        var options = WithRoutes(Route("GET", template, field));

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        result.Succeeded.Should().BeTrue(string.Join("; ", result.Failures ?? []));
    }

    /// <summary>
    /// Ensures that a route template that does not parse is rejected.
    /// </summary>
    [Theory]
    [InlineData("v1/items", "must start with '/'")]
    [InlineData("", "must not be empty")]
    [InlineData("/v1/items/{id", "contains an unclosed '{'")]
    [InlineData("/v1/items/id}", "contains an unmatched '}'")]
    [InlineData("/v1/items/{{id}}", "contains a nested '{'")]
    [InlineData("/v1/items/{}", "contains a variable with an empty name")]
    [InlineData("/v1/items/{=*}", "contains a variable with an empty name")]
    [InlineData("/v1/{id}/{id}", "declares variable 'id' more than once")]
    public void Validate_WithMalformedRouteTemplate_Fails(string template, string expectedReason)
    {
        // Arrange
        var options = WithRoutes(Route("GET", template, "id"));

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        result.Failed.Should().BeTrue();
        result.Failures.Should().ContainSingle()
            .Which.Should().Contain("Routes[0].Template").And.Contain(expectedReason);
    }

    /// <summary>
    /// Ensures that a route template with a null value is rejected as not parsing.
    /// </summary>
    [Fact]
    public void Validate_WithNullRouteTemplate_Fails()
    {
        // Arrange
        var options = WithRoutes(Route("GET", null, "id"));

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        result.Failed.Should().BeTrue();
        result.Failures.Should().ContainSingle()
            .Which.Should().Contain("Routes[0].Template").And.Contain("must not be empty");
    }

    /// <summary>
    /// Ensures that a path variable referencing a field the request message does not have is rejected.
    /// </summary>
    [Fact]
    public void Validate_WithPathVariableNotInRequestFields_Fails()
    {
        // Arrange
        var options = WithRoutes(Route("GET", "/v1/items/{itemId}", "id"));

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        result.Failed.Should().BeTrue();
        result.Failures.Should().ContainSingle()
            .Which.Should().Contain("variable '{itemId}'").And.Contain("does not match a request field");
    }

    /// <summary>
    /// Ensures that the same HTTP verb and route shape declared twice is rejected, even under different variable names.
    /// </summary>
    [Fact]
    public void Validate_WithDuplicateRouteAcrossServices_Fails()
    {
        // Arrange
        var options = WithRoutes(
            Route("GET", "/v1/items/{id}", "id", service: "items.v1.ItemService", method: "GetItem"),
            Route("GET", "/v1/items/{itemId}", "itemId", service: "items.v1.ArchiveService", method: "GetArchivedItem"));

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        result.Failed.Should().BeTrue();
        result.Failures.Should().ContainSingle()
            .Which.Should().Contain("Routes[1]").And.Contain("duplicates")
            .And.Contain("items.v1.ItemService/GetItem");
    }

    /// <summary>
    /// Ensures that the same template under different HTTP verbs is accepted, as REST routes commonly share paths.
    /// </summary>
    [Fact]
    public void Validate_WithSameTemplateDifferentVerbs_Succeeds()
    {
        // Arrange
        var options = WithRoutes(
            Route("GET", "/v1/items", "id", method: "ListItems"),
            Route("POST", "/v1/items", "id", method: "CreateItem"));

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        result.Succeeded.Should().BeTrue();
    }

    /// <summary>
    /// Ensures that an HTTP verb outside the allowed set is rejected.
    /// </summary>
    [Theory]
    [InlineData("get")]
    [InlineData("HEAD")]
    [InlineData("")]
    public void Validate_WithUnsupportedHttpMethod_Fails(string httpMethod)
    {
        // Arrange
        var options = WithRoutes(Route(httpMethod, "/v1/items/{id}", "id"));

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        result.Failed.Should().BeTrue();
        result.Failures.Should().ContainSingle()
            .Which.Should().Contain("Routes[0].HttpMethod must be one of GET, PUT, POST, DELETE, PATCH");
    }

    /// <summary>
    /// Ensures that every failing rule is reported in one result, not just the first.
    /// </summary>
    [Fact]
    public void Validate_WithMultipleProblems_ReportsAllFailures()
    {
        // Arrange
        var options = new HttpTranscoderOptions
        {
            RoutePrefix = "api/",
            JsonSerializerOptions = null,
            MaxRequestBodySize = 0,
            Routes =
            [
                Route("GET", "/v1/items/{id}", "id"),
                Route("GET", "/v1/items/{id}", "id", method: "GetItemAgain"),
                Route("PUT", "/v1/items/{missing}", "id")
            ]
        };

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        result.Failed.Should().BeTrue();
        result.Failures.Should().HaveCount(5);
        result.FailureMessage.Should()
            .Contain("HttpTranscoderOptions.RoutePrefix")
            .And.Contain("HttpTranscoderOptions.JsonSerializerOptions")
            .And.Contain("HttpTranscoderOptions.MaxRequestBodySize")
            .And.Contain("Routes[1]")
            .And.Contain("Routes[2].Template");
    }

    private static HttpTranscoderOptions WithRoutes(params HttpRouteBinding[] routes) =>
        new() { Routes = [.. routes] };

    private static HttpRouteBinding Route(
        string httpMethod,
        string? template,
        string requestField,
        string service = "items.v1.ItemService",
        string method = "GetItem") =>
        new()
        {
            ServiceName = service,
            MethodName = method,
            HttpMethod = httpMethod,
            Template = template,
            RequestFieldNames = [requestField]
        };
}

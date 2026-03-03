#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using FluentAssertions;
using GrpcWebBridge.Domain;
using GrpcWebBridge.Domain.Exceptions;
using GrpcWebBridge.Services;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace GrpcWebBridge.Tests;

/// <summary>
/// Tests for the AuthenticationService class.
/// </summary>
public sealed class AuthenticationServiceTests
{
    private readonly ILogger<AuthenticationService> _mockLogger;
    private readonly AuthenticationService _service;

    /// <summary>
    /// Initializes a new instance of the AuthenticationServiceTests class.
    /// </summary>
    public AuthenticationServiceTests()
    {
        _mockLogger = Substitute.For<ILogger<AuthenticationService>>();
        _service = new AuthenticationService(_mockLogger);
    }

    /// <summary>
    /// Tests the AuthenticateApiKey method with valid credentials.
    /// </summary>
    /// <returns>No return value.</returns>
    [Fact]
    public void AuthenticateApiKey_WithValidCredentials_ReturnsAuthenticatedContext()
    {
        // Arrange & Act
        var context = _service.AuthenticateApiKey("sk-test-api-key", "user-42");

        // Assert
        context.IsAuthenticated.Should().BeTrue();
        context.UserId.Should().Be("user-42");
        context.Scheme.Should().Be(AuthenticationScheme.ApiKey);
        context.IsExpired.Should().BeFalse();
    }

    /// <summary>
    /// Tests the AuthenticateApiKey method with an empty API key.
    /// </summary>
    /// <returns>No return value.</returns>
    [Fact]
    public void AuthenticateApiKey_WithEmptyKey_ThrowsGrpcWebBridgeException()
    {
        // Arrange & Act
        var act = () => _service.AuthenticateApiKey(string.Empty, "user-42");

        // Assert
        act.Should().Throw<GrpcWebBridgeException>()
           .Which.ErrorCode.Should().Be("INVALID_API_KEY");
    }

    /// <summary>
    /// Tests the AuthenticateCustom method with credentials.
    /// </summary>
    /// <returns>No return value.</returns>
    [Fact]
    public void AuthenticateCustom_WithCredentials_AddsClaimsToContext()
    {
        // Arrange
        var credentials = new Dictionary<string, string>
        {
            ["tenant_id"] = "acme-corp",
            ["region"] = "us-east-1"
        };

        // Act
        var context = _service.AuthenticateCustom("svc-account", credentials);

        // Assert
        context.IsAuthenticated.Should().BeTrue();
        context.GetClaim("tenant_id").Should().Be("acme-corp");
        context.GetClaim("region").Should().Be("us-east-1");
    }

    /// <summary>
    /// Tests the AuthenticateCustom method with empty credentials.
    /// </summary>
    /// <returns>No return value.</returns>
    [Fact]
    public void AuthenticateCustom_WithEmptyCredentials_ThrowsGrpcWebBridgeException()
    {
        // Arrange & Act
        var act = () => _service.AuthenticateCustom("user", new Dictionary<string, string>());

        // Assert
        act.Should().Throw<GrpcWebBridgeException>()
           .Which.ErrorCode.Should().Be("INVALID_CREDENTIALS");
    }

    /// <summary>
    /// Tests the ValidateContext method with a null context.
    /// </summary>
    /// <returns>No return value.</returns>
    [Fact]
    public void ValidateContext_WithNullContext_ReturnsFalse()
    {
        // Arrange & Act
        var result = _service.ValidateContext(null!);

        // Assert
        result.Should().BeFalse();
    }

    /// <summary>
    /// Tests the AuthorizeRole method with a context holding a matching role.
    /// </summary>
    /// <returns>No return value.</returns>
    [Fact]
    public void AuthorizeRole_WithContextHoldingMatchingRole_ReturnsTrue()
    {
        // Arrange
        var context = _service.AuthenticateApiKey("key-abc", "admin-user");
        context.AddRole("administrator");

        // Act
        var result = _service.AuthorizeRole(context, "administrator");

        // Assert
        result.Should().BeTrue();
    }

    /// <summary>
    /// Tests the ExtractBearerToken method with a header containing a bearer token.
    /// </summary>
    /// <returns>No return value.</returns>
    [Fact]
    public void ExtractBearerToken_WithBearerPrefix_ReturnsRawToken()
    {
        // Arrange
        const string header = "Bearer eyJhbGciOiJIUzI1NiJ9";

        // Act
        var token = _service.ExtractBearerToken(header);

        // Assert
        token.Should().Be("eyJhbGciOiJIUzI1NiJ9");
    }

    /// <summary>
    /// Tests the ExtractBearerToken method with a null header.
    /// </summary>
    /// <returns>No return value.</returns>
    [Fact]
    public void ExtractBearerToken_WithNullHeader_ReturnsNull()
    {
        // Arrange & Act
        var token = _service.ExtractBearerToken(null);

        // Assert
        token.Should().BeNull();
    }
}

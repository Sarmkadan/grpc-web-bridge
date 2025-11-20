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

public sealed class AuthenticationServiceTests
{
    private readonly ILogger<AuthenticationService> _mockLogger;
    private readonly AuthenticationService _service;

    public AuthenticationServiceTests()
    {
        _mockLogger = Substitute.For<ILogger<AuthenticationService>>();
        _service = new AuthenticationService(_mockLogger);
    }

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

    [Fact]
    public void AuthenticateApiKey_WithEmptyKey_ThrowsGrpcWebBridgeException()
    {
        // Arrange & Act
        var act = () => _service.AuthenticateApiKey(string.Empty, "user-42");

        // Assert
        act.Should().Throw<GrpcWebBridgeException>()
           .Which.ErrorCode.Should().Be("INVALID_API_KEY");
    }

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

    [Fact]
    public void AuthenticateCustom_WithEmptyCredentials_ThrowsGrpcWebBridgeException()
    {
        // Arrange & Act
        var act = () => _service.AuthenticateCustom("user", new Dictionary<string, string>());

        // Assert
        act.Should().Throw<GrpcWebBridgeException>()
           .Which.ErrorCode.Should().Be("INVALID_CREDENTIALS");
    }

    [Fact]
    public void ValidateContext_WithNullContext_ReturnsFalse()
    {
        // Arrange & Act
        var result = _service.ValidateContext(null!);

        // Assert
        result.Should().BeFalse();
    }

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

    [Fact]
    public void ExtractBearerToken_WithNullHeader_ReturnsNull()
    {
        // Arrange & Act
        var token = _service.ExtractBearerToken(null);

        // Assert
        token.Should().BeNull();
    }
}

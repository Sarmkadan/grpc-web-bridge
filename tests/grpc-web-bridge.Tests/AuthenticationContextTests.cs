#nullable enable

using FluentAssertions;
using GrpcWebBridge.Domain;
using GrpcWebBridge.Domain.Models;
using Xunit;

namespace GrpcWebBridge.Tests;

public sealed class AuthenticationContextTests
{
    [Fact]
    public void Constructor_WithValidParameters_SetsPropertiesCorrectly()
    {
        // Arrange
        var userId = "user123";
        var scheme = AuthenticationScheme.Bearer;
        var token = "token123";

        // Act
        var context = new AuthenticationContext(userId, scheme, token);

        // Assert
        context.UserId.Should().Be(userId);
        context.Scheme.Should().Be(scheme);
        context.Token.Should().Be(token);
        context.IsAuthenticated.Should().BeTrue();
        context.Id.Should().NotBeNullOrEmpty();
        context.AuthenticatedAt.Should().BeCloseTo(DateTime.UtcNow, precision: TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Constructor_WithNullUserId_ThrowsArgumentException()
    {
        // Arrange
        string? userId = null;
        var scheme = AuthenticationScheme.Bearer;

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new AuthenticationContext(userId!, scheme));
    }

    [Fact]
    public void Constructor_WithEmptyUserId_ThrowsArgumentException()
    {
        // Arrange
        var userId = "";
        var scheme = AuthenticationScheme.Bearer;

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new AuthenticationContext(userId, scheme));
    }

    [Fact]
    public void Constructor_WithWhitespaceUserId_ThrowsArgumentException()
    {
        // Arrange
        var userId = "   ";
        var scheme = AuthenticationScheme.Bearer;

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new AuthenticationContext(userId, scheme));
    }

    [Fact]
    public void AddRole_WithValidRole_AddsToRolesCollection()
    {
        // Arrange
        var context = new AuthenticationContext("user123", AuthenticationScheme.Bearer);
        var role = "admin";

        // Act
        context.AddRole(role);

        // Assert
        context.Roles.Should().Contain(role);
        context.Roles.Should().HaveCount(1);
    }

    [Fact]
    public void AddRole_WithDuplicateRole_DoesNotAddDuplicate()
    {
        // Arrange
        var context = new AuthenticationContext("user123", AuthenticationScheme.Bearer);
        var role = "admin";

        // Act
        context.AddRole(role);
        context.AddRole(role);

        // Assert
        context.Roles.Should().HaveCount(1);
    }

    [Fact]
    public void AddRole_WithNullRole_ThrowsArgumentException()
    {
        // Arrange
        var context = new AuthenticationContext("user123", AuthenticationScheme.Bearer);
        string? role = null;

        // Act & Assert
        Assert.Throws<ArgumentException>(() => context.AddRole(role!));
    }

    [Fact]
    public void AddRole_WithEmptyRole_ThrowsArgumentException()
    {
        // Arrange
        var context = new AuthenticationContext("user123", AuthenticationScheme.Bearer);
        var role = "";

        // Act & Assert
        Assert.Throws<ArgumentException>(() => context.AddRole(role));
    }

    [Fact]
    public void HasRole_WithExistingRole_ReturnsTrue()
    {
        // Arrange
        var context = new AuthenticationContext("user123", AuthenticationScheme.Bearer);
        var role = "admin";
        context.AddRole(role);

        // Act
        var result = context.HasRole(role);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void HasRole_WithNonExistingRole_ReturnsFalse()
    {
        // Arrange
        var context = new AuthenticationContext("user123", AuthenticationScheme.Bearer);
        context.AddRole("admin");

        // Act
        var result = context.HasRole("user");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void HasRole_WithNullRole_ReturnsFalse()
    {
        // Arrange
        var context = new AuthenticationContext("user123", AuthenticationScheme.Bearer);

        // Act
        var result = context.HasRole(null!);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void HasRole_WithEmptyRole_ReturnsFalse()
    {
        // Arrange
        var context = new AuthenticationContext("user123", AuthenticationScheme.Bearer);

        // Act
        var result = context.HasRole("");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void HasAnyRole_WithMatchingRole_ReturnsTrue()
    {
        // Arrange
        var context = new AuthenticationContext("user123", AuthenticationScheme.Bearer);
        context.AddRole("admin");
        context.AddRole("user");

        // Act
        var result = context.HasAnyRole("guest", "admin", "moderator");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void HasAnyRole_WithNoMatchingRoles_ReturnsFalse()
    {
        // Arrange
        var context = new AuthenticationContext("user123", AuthenticationScheme.Bearer);
        context.AddRole("admin");

        // Act
        var result = context.HasAnyRole("guest", "user", "moderator");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void HasAnyRole_WithEmptyRolesArray_ReturnsFalse()
    {
        // Arrange
        var context = new AuthenticationContext("user123", AuthenticationScheme.Bearer);

        // Act
        var result = context.HasAnyRole();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void HasAllRoles_WithAllMatchingRoles_ReturnsTrue()
    {
        // Arrange
        var context = new AuthenticationContext("user123", AuthenticationScheme.Bearer);
        context.AddRole("admin");
        context.AddRole("user");

        // Act
        var result = context.HasAllRoles("admin", "user");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void HasAllRoles_WithMissingRole_ReturnsFalse()
    {
        // Arrange
        var context = new AuthenticationContext("user123", AuthenticationScheme.Bearer);
        context.AddRole("admin");

        // Act
        var result = context.HasAllRoles("admin", "user");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void HasAllRoles_WithEmptyRolesArray_ReturnsFalse()
    {
        // Arrange
        var context = new AuthenticationContext("user123", AuthenticationScheme.Bearer);

        // Act
        var result = context.HasAllRoles();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void AddClaim_WithValidKeyValue_AddsToClaimsDictionary()
    {
        // Arrange
        var context = new AuthenticationContext("user123", AuthenticationScheme.Bearer);
        var key = "permission";
        var value = "read";

        // Act
        context.AddClaim(key, value);

        // Assert
        context.Claims.Should().ContainKey(key);
        context.Claims[key].Should().Be(value);
    }

    [Fact]
    public void AddClaim_WithNullKey_ThrowsArgumentException()
    {
        // Arrange
        var context = new AuthenticationContext("user123", AuthenticationScheme.Bearer);
        string? key = null;
        var value = "read";

        // Act & Assert
        Assert.Throws<ArgumentException>(() => context.AddClaim(key!, value));
    }

    [Fact]
    public void AddClaim_WithEmptyKey_ThrowsArgumentException()
    {
        // Arrange
        var context = new AuthenticationContext("user123", AuthenticationScheme.Bearer);
        var key = "";
        var value = "read";

        // Act & Assert
        Assert.Throws<ArgumentException>(() => context.AddClaim(key, value));
    }

    [Fact]
    public void GetClaim_WithExistingKey_ReturnsValue()
    {
        // Arrange
        var context = new AuthenticationContext("user123", AuthenticationScheme.Bearer);
        var key = "permission";
        var value = "read";
        context.AddClaim(key, value);

        // Act
        var result = context.GetClaim(key);

        // Assert
        result.Should().Be(value);
    }

    [Fact]
    public void GetClaim_WithNonExistingKey_ReturnsNull()
    {
        // Arrange
        var context = new AuthenticationContext("user123", AuthenticationScheme.Bearer);

        // Act
        var result = context.GetClaim("nonexistent");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void HasClaim_WithExistingKey_ReturnsTrue()
    {
        // Arrange
        var context = new AuthenticationContext("user123", AuthenticationScheme.Bearer);
        var key = "permission";
        context.AddClaim(key, "read");

        // Act
        var result = context.HasClaim(key);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void HasClaim_WithNonExistingKey_ReturnsFalse()
    {
        // Arrange
        var context = new AuthenticationContext("user123", AuthenticationScheme.Bearer);

        // Act
        var result = context.HasClaim("nonexistent");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void SetExpiration_WithPositiveMinutes_SetsExpiresAt()
    {
        // Arrange
        var context = new AuthenticationContext("user123", AuthenticationScheme.Bearer);
        var minutes = 30;

        // Act
        context.SetExpiration(minutes);

        // Assert
        context.ExpiresAt.Should().BeCloseTo(DateTime.UtcNow.AddMinutes(minutes), precision: TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void SetExpiration_WithZeroMinutes_ThrowsArgumentException()
    {
        // Arrange
        var context = new AuthenticationContext("user123", AuthenticationScheme.Bearer);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => context.SetExpiration(0));
    }

    [Fact]
    public void SetExpiration_WithNegativeMinutes_ThrowsArgumentException()
    {
        // Arrange
        var context = new AuthenticationContext("user123", AuthenticationScheme.Bearer);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => context.SetExpiration(-5));
    }

    [Fact]
    public void SetExpiration_WithFutureDateTime_SetsExpiresAt()
    {
        // Arrange
        var context = new AuthenticationContext("user123", AuthenticationScheme.Bearer);
        var expirationTime = DateTime.UtcNow.AddHours(1);

        // Act
        context.SetExpiration(expirationTime);

        // Assert
        context.ExpiresAt.Should().Be(expirationTime);
    }

    [Fact]
    public void SetExpiration_WithPastDateTime_ThrowsArgumentException()
    {
        // Arrange
        var context = new AuthenticationContext("user123", AuthenticationScheme.Bearer);
        var expirationTime = DateTime.UtcNow.AddHours(-1);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => context.SetExpiration(expirationTime));
    }

    [Fact]
    public void GetRemainingTime_WithNoExpiration_ReturnsMaxValue()
    {
        // Arrange
        var context = new AuthenticationContext("user123", AuthenticationScheme.Bearer);

        // Act
        var result = context.GetRemainingTime();

        // Assert
        result.Should().Be(TimeSpan.MaxValue);
    }

    [Fact]
    public void GetRemainingTime_WithFutureExpiration_ReturnsPositiveTimeSpan()
    {
        // Arrange
        var context = new AuthenticationContext("user123", AuthenticationScheme.Bearer);
        context.SetExpiration(60); // 1 hour

        // Act
        var result = context.GetRemainingTime();

        // Assert
        result.Should().BeCloseTo(TimeSpan.FromMinutes(60), precision: TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void GetRemainingTime_WithExpiredContext_ReturnsZero()
    {
        // Arrange
        var context = new AuthenticationContext("user123", AuthenticationScheme.Bearer);
        var pastTime = DateTime.UtcNow.AddMinutes(-1);
        context.ExpiresAt = pastTime;

        // Act
        var result = context.GetRemainingTime();

        // Assert
        result.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void IsExpired_WithNoExpiration_ReturnsFalse()
    {
        // Arrange
        var context = new AuthenticationContext("user123", AuthenticationScheme.Bearer);

        // Act
        var result = context.IsExpired;

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsExpired_WithFutureExpiration_ReturnsFalse()
    {
        // Arrange
        var context = new AuthenticationContext("user123", AuthenticationScheme.Bearer);
        context.SetExpiration(60); // 1 hour in future

        // Act
        var result = context.IsExpired;

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsExpired_WithPastExpiration_ReturnsTrue()
    {
        // Arrange
        var context = new AuthenticationContext("user123", AuthenticationScheme.Bearer);
        var pastTime = DateTime.UtcNow.AddMinutes(-1);
        context.ExpiresAt = pastTime;

        // Act
        var result = context.IsExpired;

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void AddCustomData_WithValidKeyValue_AddsToCustomDataDictionary()
    {
        // Arrange
        var context = new AuthenticationContext("user123", AuthenticationScheme.Bearer);
        var key = "customKey";
        var value = 42;

        // Act
        context.AddCustomData(key, value);

        // Assert
        context.CustomData.Should().ContainKey(key);
        context.CustomData[key].Should().Be(value);
    }

    [Fact]
    public void AddCustomData_WithNullKey_ThrowsArgumentException()
    {
        // Arrange
        var context = new AuthenticationContext("user123", AuthenticationScheme.Bearer);
        string? key = null;
        var value = 42;

        // Act & Assert
        Assert.Throws<ArgumentException>(() => context.AddCustomData(key!, value));
    }

    [Fact]
    public void AddCustomData_WithEmptyKey_ThrowsArgumentException()
    {
        // Arrange
        var context = new AuthenticationContext("user123", AuthenticationScheme.Bearer);
        var key = "";
        var value = 42;

        // Act & Assert
        Assert.Throws<ArgumentException>(() => context.AddCustomData(key, value));
    }

    [Fact]
    public void GetCustomData_WithExistingKey_ReturnsValue()
    {
        // Arrange
        var context = new AuthenticationContext("user123", AuthenticationScheme.Bearer);
        var key = "customKey";
        var value = 42;
        context.AddCustomData(key, value);

        // Act
        var result = context.GetCustomData(key);

        // Assert
        result.Should().Be(value);
    }

    [Fact]
    public void GetCustomData_WithNonExistingKey_ReturnsNull()
    {
        // Arrange
        var context = new AuthenticationContext("user123", AuthenticationScheme.Bearer);

        // Act
        var result = context.GetCustomData("nonexistent");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void Validate_WithValidAuthenticatedContext_DoesNotThrow()
    {
        // Arrange
        var context = new AuthenticationContext("user123", AuthenticationScheme.Bearer, "token123");

        // Act
        Action act = () => context.Validate();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_WithUnauthenticatedContext_NoUserId_ThrowsArgumentException()
    {
        // Arrange
        var context = new AuthenticationContext
        {
            IsAuthenticated = false,
            UserId = null!,
            Scheme = AuthenticationScheme.Bearer
        };

        // Act & Assert
        Assert.Throws<ArgumentException>(() => context.Validate());
    }

    [Fact]
    public void Validate_WithUnauthenticatedContext_NoTokenForBearer_ThrowsArgumentException()
    {
        // Arrange
        var context = new AuthenticationContext
        {
            IsAuthenticated = false,
            UserId = "user123",
            Scheme = AuthenticationScheme.Bearer,
            Token = null
        };

        // Act & Assert
        Assert.Throws<ArgumentException>(() => context.Validate());
    }

    [Fact]
    public void Validate_WithUnauthenticatedContext_NoTokenForApiKey_ThrowsArgumentException()
    {
        // Arrange
        var context = new AuthenticationContext
        {
            IsAuthenticated = false,
            UserId = "user123",
            Scheme = AuthenticationScheme.ApiKey,
            Token = null
        };

        // Act & Assert
        Assert.Throws<ArgumentException>(() => context.Validate());
    }

    [Fact]
    public void Validate_WithUnauthenticatedContext_NoTokenForNoneScheme_DoesNotThrow()
    {
        // Arrange
        var context = new AuthenticationContext
        {
            IsAuthenticated = false,
            UserId = "user123",
            Scheme = AuthenticationScheme.None,
            Token = null
        };

        // Act
        Action act = () => context.Validate();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_WithExpiredContext_ThrowsInvalidOperationException()
    {
        // Arrange
        var context = new AuthenticationContext("user123", AuthenticationScheme.Bearer, "token123");
        var pastTime = DateTime.UtcNow.AddMinutes(-1);
        context.ExpiresAt = pastTime;

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => context.Validate());
    }

    [Fact]
    public void ToString_ReturnsExpectedFormat()
    {
        // Arrange
        var context = new AuthenticationContext("user123", AuthenticationScheme.Bearer, "token123");

        // Act
        var result = context.ToString();

        // Assert
        result.Should().StartWith("AuthContext");
        result.Should().Contain(AuthenticationScheme.Bearer.ToString());
        result.Should().Contain("user123");
    }

    [Fact]
    public void Equals_SameId_ReturnsTrue()
    {
        // Arrange
        var id = "same-id";
        var context1 = new AuthenticationContext("user123", AuthenticationScheme.Bearer) { Id = id };
        var context2 = new AuthenticationContext("user456", AuthenticationScheme.ApiKey) { Id = id };

        // Act
        var result = context1.Equals(context2);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Equals_DifferentId_ReturnsFalse()
    {
        // Arrange
        var context1 = new AuthenticationContext("user123", AuthenticationScheme.Bearer) { Id = "id1" };
        var context2 = new AuthenticationContext("user456", AuthenticationScheme.ApiKey) { Id = "id2" };

        // Act
        var result = context1.Equals(context2);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Equals_Null_ReturnsFalse()
    {
        // Arrange
        var context = new AuthenticationContext("user123", AuthenticationScheme.Bearer);

        // Act
        var result = context.Equals(null);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void GetHashCode_SameId_ReturnsSameHashCode()
    {
        // Arrange
        var id = "same-id";
        var context1 = new AuthenticationContext("user123", AuthenticationScheme.Bearer) { Id = id };
        var context2 = new AuthenticationContext("user456", AuthenticationScheme.ApiKey) { Id = id };

        // Act
        var hash1 = context1.GetHashCode();
        var hash2 = context2.GetHashCode();

        // Assert
        hash1.Should().Be(hash2);
    }
}
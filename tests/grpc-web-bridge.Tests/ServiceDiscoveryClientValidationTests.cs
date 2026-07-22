#nullable enable

using FluentAssertions;
using GrpcWebBridge.Integration;
using Xunit;

namespace GrpcWebBridge.Tests;

/// <summary>
/// Contains unit tests for <see cref="ServiceDiscoveryClientValidation"/> ensuring that
/// service instance validation behaves as expected for all public methods.
/// </summary>
public sealed class ServiceDiscoveryClientValidationTests
{
    /// <summary>
    /// Tests that Validate returns an empty list for a completely valid ServiceInstance.
    /// </summary>
    [Fact]
    public void Validate_WithValidServiceInstance_ReturnsEmptyList()
    {
        // Arrange
        var validInstance = new ServiceInstance
        {
            Id = "service-1",
            Name = "TestService",
            Host = "localhost",
            Port = 5000,
            Status = "UP",
            RegisteredAt = DateTime.UtcNow.AddMinutes(-1),
            LastHeartbeat = DateTime.UtcNow.AddSeconds(-30)
        };

        // Act
        var problems = validInstance.Validate();

        // Assert
        problems.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that Validate returns appropriate errors for a null ServiceInstance.
    /// </summary>
    [Fact]
    public void Validate_WithNullServiceInstance_ThrowsArgumentNullException()
    {
        // Arrange
        ServiceInstance? nullInstance = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => nullInstance!.Validate());
    }

    /// <summary>
    /// Tests that Validate catches all common validation issues in a single instance.
    /// </summary>
    [Fact]
    public void Validate_WithMultipleProblems_ReturnsAllProblems()
    {
        // Arrange
        var invalidInstance = new ServiceInstance
        {
            Id = "",
            Name = "   ",
            Host = null,
            Port = 70000,
            Status = "INVALID_STATUS",
            RegisteredAt = default,
            LastHeartbeat = DateTime.UtcNow.AddMinutes(10),
            Metadata = new Dictionary<string, string>
            {
                { "", "value" },
                { "key", "" }
            }
        };

        // Act
        var problems = invalidInstance.Validate();

        // Assert
        problems.Should().HaveCount(8); // 7 main validations + 1 metadata issue (breaks after first metadata error)
        problems.Should().Contain("ServiceInstance.Id cannot be null, empty, or whitespace.");
        problems.Should().Contain("ServiceInstance.Name cannot be null, empty, or whitespace.");
        problems.Should().Contain("ServiceInstance.Host cannot be null, empty, or whitespace.");
        problems.Should().Contain("ServiceInstance.Port must be between 0 and 65535, but was 70000.");
        problems.Should().Contain("ServiceInstance.Status must be a valid status (e.g., 'UP', 'DOWN', 'MAINTENANCE', 'OUT_OF_SERVICE', 'UNKNOWN'), but was 'INVALID_STATUS'.");
        problems.Should().Contain("ServiceInstance.RegisteredAt cannot be default(DateTime).");
        problems.Should().Contain(p => p.Contains("LastHeartbeat cannot be in the future"));
        problems.Should().Contain("ServiceInstance.Metadata contains a key that is null, empty, or whitespace.");
    }

    /// <summary>
    /// Tests that Validate handles null metadata gracefully.
    /// </summary>
    [Fact]
    public void Validate_WithNullMetadata_DoesNotAddMetadataProblems()
    {
        // Arrange
        var instance = new ServiceInstance
        {
            Id = "service-1",
            Name = "TestService",
            Host = "localhost",
            Port = 5000,
            Status = "UP",
            RegisteredAt = DateTime.UtcNow.AddMinutes(-1),
            Metadata = null
        };

        // Act
        var problems = instance.Validate();

        // Assert
        problems.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that Validate handles empty metadata dictionary.
    /// </summary>
    [Fact]
    public void Validate_WithEmptyMetadata_DoesNotAddProblems()
    {
        // Arrange
        var instance = new ServiceInstance
        {
            Id = "service-1",
            Name = "TestService",
            Host = "localhost",
            Port = 5000,
            Status = "UP",
            RegisteredAt = DateTime.UtcNow.AddMinutes(-1),
            Metadata = new Dictionary<string, string>()
        };

        // Act
        var problems = instance.Validate();

        // Assert
        problems.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that Validate handles boundary values for Port correctly.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(32768)]
    [InlineData(65535)]
    public void Validate_WithValidPortNumbers_DoesNotAddPortProblems(int port)
    {
        // Arrange
        var instance = new ServiceInstance
        {
            Id = "service-1",
            Name = "TestService",
            Host = "localhost",
            Port = port,
            Status = "UP",
            RegisteredAt = DateTime.UtcNow.AddMinutes(-1)
        };

        // Act
        var problems = instance.Validate();

        // Assert
        problems.Should().NotContain(p => p.Contains("Port"));
    }

    /// <summary>
    /// Tests that Validate handles invalid port numbers.
    /// </summary>
    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    [InlineData(65536)]
    [InlineData(70000)]
    public void Validate_WithInvalidPortNumbers_ReturnsPortProblems(int port)
    {
        // Arrange
        var instance = new ServiceInstance
        {
            Id = "service-1",
            Name = "TestService",
            Host = "localhost",
            Port = port,
            Status = "UP",
            RegisteredAt = DateTime.UtcNow.AddMinutes(-1)
        };

        // Act
        var problems = instance.Validate();

        // Assert
        problems.Should().ContainSingle(p => p.Contains("Port must be between 0 and 65535"));
    }

    /// <summary>
    /// Tests that Validate handles all valid status values.
    /// </summary>
    [Theory]
    [InlineData("UP")]
    [InlineData("DOWN")]
    [InlineData("MAINTENANCE")]
    [InlineData("OUT_OF_SERVICE")]
    [InlineData("UNKNOWN")]
    [InlineData("up")] // Case insensitive
    [InlineData("  UP  ")] // With whitespace
    public void Validate_WithValidStatuses_DoesNotAddStatusProblems(string status)
    {
        // Arrange
        var instance = new ServiceInstance
        {
            Id = "service-1",
            Name = "TestService",
            Host = "localhost",
            Port = 5000,
            Status = status,
            RegisteredAt = DateTime.UtcNow.AddMinutes(-1)
        };

        // Act
        var problems = instance.Validate();

        // Assert
        problems.Should().NotContain(p => p.Contains("Status"));
    }

    /// <summary>
    /// Tests that Validate handles invalid status values.
    /// </summary>
    [Theory]
    [InlineData("INVALID")]
    [InlineData("UP_UP")]
    [InlineData("STARTING")]
    [InlineData("READY")]
    public void Validate_WithInvalidStatuses_ReturnsStatusProblems(string status)
    {
        // Arrange
        var instance = new ServiceInstance
        {
            Id = "service-1",
            Name = "TestService",
            Host = "localhost",
            Port = 5000,
            Status = status,
            RegisteredAt = DateTime.UtcNow.AddMinutes(-1)
        };

        // Act
        var problems = instance.Validate();

        // Assert
        problems.Should().ContainSingle(p => p.Contains("Status must be a valid status"));
    }

    /// <summary>
    /// Tests that Validate handles future LastHeartbeat dates.
    /// </summary>
    [Fact]
    public void Validate_WithFutureLastHeartbeat_ReturnsProblem()
    {
        // Arrange
        var instance = new ServiceInstance
        {
            Id = "service-1",
            Name = "TestService",
            Host = "localhost",
            Port = 5000,
            Status = "UP",
            RegisteredAt = DateTime.UtcNow.AddMinutes(-1),
            LastHeartbeat = DateTime.UtcNow.AddMinutes(10)
        };

        // Act
        var problems = instance.Validate();

        // Assert
        problems.Should().ContainSingle(p => p.Contains("LastHeartbeat cannot be in the future"));
    }

    /// <summary>
    /// Tests that Validate handles default LastHeartbeat.
    /// </summary>
    [Fact]
    public void Validate_WithDefaultLastHeartbeat_ReturnsProblem()
    {
        // Arrange
        var instance = new ServiceInstance
        {
            Id = "service-1",
            Name = "TestService",
            Host = "localhost",
            Port = 5000,
            Status = "UP",
            RegisteredAt = DateTime.UtcNow.AddMinutes(-1),
            LastHeartbeat = DateTime.MinValue // default(DateTime) value
        };

        // Act
        var problems = instance.Validate();

        // Assert
        problems.Should().ContainSingle(p => p.Contains("LastHeartbeat cannot be default(DateTime) when set"));
    }

    /// <summary>
    /// Tests that IsValid returns true for a valid ServiceInstance.
    /// </summary>
    [Fact]
    public void IsValid_WithValidServiceInstance_ReturnsTrue()
    {
        // Arrange
        var validInstance = new ServiceInstance
        {
            Id = "service-1",
            Name = "TestService",
            Host = "localhost",
            Port = 5000,
            Status = "UP",
            RegisteredAt = DateTime.UtcNow.AddMinutes(-1)
        };

        // Act
        var isValid = validInstance.IsValid();

        // Assert
        isValid.Should().BeTrue();
    }

    /// <summary>
    /// Tests that IsValid returns false for an invalid ServiceInstance.
    /// </summary>
    [Fact]
    public void IsValid_WithInvalidServiceInstance_ReturnsFalse()
    {
        // Arrange
        var invalidInstance = new ServiceInstance
        {
            Id = "",
            Name = "TestService",
            Host = "localhost",
            Port = 5000,
            Status = "UP",
            RegisteredAt = DateTime.UtcNow.AddMinutes(-1)
        };

        // Act
        var isValid = invalidInstance.IsValid();

        // Assert
        isValid.Should().BeFalse();
    }

    /// <summary>
    /// Tests that EnsureValid does not throw for a valid ServiceInstance.
    /// </summary>
    [Fact]
    public void EnsureValid_WithValidServiceInstance_DoesNotThrow()
    {
        // Arrange
        var validInstance = new ServiceInstance
        {
            Id = "service-1",
            Name = "TestService",
            Host = "localhost",
            Port = 5000,
            Status = "UP",
            RegisteredAt = DateTime.UtcNow.AddMinutes(-1)
        };

        // Act
        Action act = () => validInstance.EnsureValid();

        // Assert
        act.Should().NotThrow();
    }

    /// <summary>
    /// Tests that EnsureValid throws ArgumentNullException for null ServiceInstance.
    /// </summary>
    [Fact]
    public void EnsureValid_WithNullServiceInstance_ThrowsArgumentNullException()
    {
        // Arrange
        ServiceInstance? nullInstance = null;

        // Act
        Action act = () => nullInstance!.EnsureValid();

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    /// <summary>
    /// Tests that EnsureValid throws ArgumentException with detailed message for invalid ServiceInstance.
    /// </summary>
    [Fact]
    public void EnsureValid_WithInvalidServiceInstance_ThrowsArgumentExceptionWithProblems()
    {
        // Arrange
        var invalidInstance = new ServiceInstance
        {
            Id = "",
            Name = "TestService",
            Host = "localhost",
            Port = 5000,
            Status = "UP",
            RegisteredAt = DateTime.UtcNow.AddMinutes(-1)
        };

        // Act
        Action act = () => invalidInstance.EnsureValid();

        // Assert
        var exception = act.Should().Throw<ArgumentException>().Which;
        exception.Message.Should().Contain("ServiceInstance is not valid");
        exception.Message.Should().Contain("ServiceInstance.Id cannot be null, empty, or whitespace");
    }
}
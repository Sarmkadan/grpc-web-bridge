#nullable enable

using FluentAssertions;
using GrpcWebBridge.Domain.Models;
using Xunit;

namespace GrpcWebBridge.Tests;

public sealed class GrpcServiceExtensionsTests
{
    [Fact]
    public void GetFullEndpoint_WithHttpService_ReturnsCorrectUrl()
    {
        // Arrange
        var service = new GrpcService("TestService", "Test.Package", "localhost", 8080)
        {
            UseTls = false
        };

        // Act
        var result = service.GetFullEndpoint();

        // Assert
        result.Should().Be("http://localhost:8080");
    }

    [Fact]
    public void GetFullEndpoint_WithHttpsService_ReturnsCorrectUrl()
    {
        // Arrange
        var service = new GrpcService("TestService", "Test.Package", "example.com", 443)
        {
            UseTls = true
        };

        // Act
        var result = service.GetFullEndpoint();

        // Assert
        result.Should().Be("https://example.com:443");
    }

    [Fact]
    public void GetFullEndpoint_WithCustomPort_ReturnsCorrectUrl()
    {
        // Arrange
        var service = new GrpcService("TestService", "Test.Package", "api.example.com", 8081)
        {
            UseTls = false
        };

        // Act
        var result = service.GetFullEndpoint();

        // Assert
        result.Should().Be("http://api.example.com:8081");
    }

    [Fact]
    public void GetFullEndpoint_NullService_ThrowsArgumentNullException()
    {
        // Arrange
        GrpcService? service = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => service!.GetFullEndpoint());
    }

    [Fact]
    public void GetMetadataValueOrDefault_WithExistingKey_ReturnsValue()
    {
        // Arrange
        var service = new GrpcService("TestService", "Test.Package", "localhost", 50051);
        service.SetMetadata("auth-token", "secret123");
        service.SetMetadata("content-type", "application/grpc");

        // Act
        var result = service.GetMetadataValueOrDefault("auth-token");

        // Assert
        result.Should().Be("secret123");
    }

    [Fact]
    public void GetMetadataValueOrDefault_WithNonExistingKey_ReturnsDefaultValue()
    {
        // Arrange
        var service = new GrpcService("TestService", "Test.Package", "localhost", 50051);
        service.SetMetadata("existing-key", "value1");

        // Act
        var result = service.GetMetadataValueOrDefault("non-existing-key", "default-value");

        // Assert
        result.Should().Be("default-value");
    }

    [Fact]
    public void GetMetadataValueOrDefault_WithEmptyDefaultValue_ReturnsEmptyString()
    {
        // Arrange
        var service = new GrpcService("TestService", "Test.Package", "localhost", 50051);
        service.SetMetadata("existing-key", "value1");

        // Act
        var result = service.GetMetadataValueOrDefault("non-existing-key");

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void GetMetadataValueOrDefault_NullService_ThrowsArgumentNullException()
    {
        // Arrange
        GrpcService? service = null;
        const string key = "test-key";

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => service!.GetMetadataValueOrDefault(key));
    }

    [Theory]
    [InlineData("")]
    public void GetMetadataValueOrDefault_EmptyKey_ThrowsArgumentException(string key)
    {
        // Arrange
        var service = new GrpcService("TestService", "Test.Package", "localhost", 50051);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => service.GetMetadataValueOrDefault(key));
    }

    [Fact]
    public void GetMetadataValueOrDefault_NullKey_ThrowsArgumentNullException()
    {
        // Arrange
        var service = new GrpcService("TestService", "Test.Package", "localhost", 50051);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => service.GetMetadataValueOrDefault(null!));
    }

    [Fact]
    public void GetAllMetadataKeys_WithEmptyMetadata_ReturnsEmptyCollection()
    {
        // Arrange
        var service = new GrpcService("TestService", "Test.Package", "localhost", 50051);

        // Act
        var result = service.GetAllMetadataKeys();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void GetAllMetadataKeys_WithMultipleKeys_ReturnsAllKeys()
    {
        // Arrange
        var service = new GrpcService("TestService", "Test.Package", "localhost", 50051);
        service.SetMetadata("key1", "value1");
        service.SetMetadata("key2", "value2");
        service.SetMetadata("key3", "value3");

        // Act
        var result = service.GetAllMetadataKeys();

        // Assert
        result.Should().HaveCount(3);
        result.Should().Contain("key1");
        result.Should().Contain("key2");
        result.Should().Contain("key3");
    }

    [Fact]
    public void GetAllMetadataKeys_ReturnsReadOnlyCollection()
    {
        // Arrange
        var service = new GrpcService("TestService", "Test.Package", "localhost", 50051);
        service.SetMetadata("key1", "value1");

        // Act
        var result = service.GetAllMetadataKeys();

        // Assert
        result.Should().BeAssignableTo<IReadOnlyCollection<string>>();
        // Verify it's actually read-only by attempting to cast
        Assert.IsAssignableFrom<IReadOnlyCollection<string>>(result);
    }

    [Fact]
    public void GetAllMetadataKeys_NullService_ThrowsArgumentNullException()
    {
        // Arrange
        GrpcService? service = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => service!.GetAllMetadataKeys());
    }

    [Fact]
    public void GetScheme_WithUseTlsTrue_ReturnsHttps()
    {
        // Arrange
        var service = new GrpcService("TestService", "Test.Package", "localhost", 443)
        {
            UseTls = true
        };

        // Act
        var result = service.GetFullEndpoint();

        // Assert
        result.Should().StartWith("https://");
    }

    [Fact]
    public void GetScheme_WithUseTlsFalse_ReturnsHttp()
    {
        // Arrange
        var service = new GrpcService("TestService", "Test.Package", "localhost", 8080)
        {
            UseTls = false
        };

        // Act
        var result = service.GetFullEndpoint();

        // Assert
        result.Should().StartWith("http://");
    }
}
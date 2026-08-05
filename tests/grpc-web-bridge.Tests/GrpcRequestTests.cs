#nullable enable
using FluentAssertions;
using GrpcWebBridge.Domain;
using GrpcWebBridge.Domain.Models;
using Xunit;

namespace GrpcWebBridge.Tests;

/// <summary>
/// Unit tests for GrpcRequest covering construction, metadata handling,
/// payload management, and validation.
/// </summary>
public sealed class GrpcRequestTests
{
    [Fact]
    public void Constructor_ValidArguments_SetsProperties()
    {
        // Arrange
        var payload = new byte[] { 1, 2, 3 };

        // Act
        var request = new GrpcRequest("MyService", "MyMethod", payload);

        // Assert
        request.ServiceName.Should().Be("MyService");
        request.MethodName.Should().Be("MyMethod");
        request.FullMethodName.Should().Be("/MyService/MyMethod");
        request.Payload.Should().BeEquivalentTo(payload);
        request.Id.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Constructor_NullPayload_DefaultsToEmptyArray()
    {
        // Act
        var request = new GrpcRequest("MyService", "MyMethod", null!);

        // Assert
        request.Payload.Should().BeEmpty();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_InvalidServiceName_ThrowsArgumentException(string? serviceName)
    {
        // Act
        var act = () => new GrpcRequest(serviceName!, "MyMethod", []);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_InvalidMethodName_ThrowsArgumentException(string? methodName)
    {
        // Act
        var act = () => new GrpcRequest("MyService", methodName!, []);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AddMetadata_ValidKey_AddsOrOverwritesEntry()
    {
        // Arrange
        var request = new GrpcRequest("Svc", "Method", []);

        // Act
        request.AddMetadata("key1", "value1");
        request.AddMetadata("key1", "value2");

        // Assert
        request.Metadata.Should().ContainKey("key1");
        request.Metadata["key1"].Should().Be("value2");
    }

    [Fact]
    public void AddMetadata_EmptyKey_ThrowsArgumentException()
    {
        // Arrange
        var request = new GrpcRequest("Svc", "Method", []);

        // Act
        var act = () => request.AddMetadata("", "value");

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void GetMetadata_ExistingAndMissingKeys_ReturnsExpectedValues()
    {
        // Arrange
        var request = new GrpcRequest("Svc", "Method", []);
        request.AddMetadata("present", "value");

        // Act & Assert
        request.GetMetadata("present").Should().Be("value");
        request.GetMetadata("absent").Should().BeNull();
        request.HasMetadata("present").Should().BeTrue();
        request.HasMetadata("absent").Should().BeFalse();
    }

    [Fact]
    public void SetPayload_ValidPayload_UpdatesPayloadAndFormat()
    {
        // Arrange
        var request = new GrpcRequest("Svc", "Method", []);
        var newPayload = new byte[] { 9, 8, 7 };

        // Act
        request.SetPayload(newPayload, SerializationFormat.Json);

        // Assert
        request.Payload.Should().BeEquivalentTo(newPayload);
        request.PayloadFormat.Should().Be(SerializationFormat.Json);
    }

    [Fact]
    public void SetPayload_NullPayload_ThrowsArgumentNullException()
    {
        // Arrange
        var request = new GrpcRequest("Svc", "Method", []);

        // Act
        var act = () => request.SetPayload(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Validate_ValidRequest_DoesNotThrow()
    {
        // Arrange
        var request = new GrpcRequest("Svc", "Method", [1, 2, 3]);

        // Act
        var act = () => request.Validate();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_PayloadExceedsMaxSize_ThrowsArgumentException()
    {
        // Arrange
        var request = new GrpcRequest("Svc", "Method", [])
        {
            Payload = new byte[Constants.Grpc.MaxMessageSize + 1]
        };

        // Act
        var act = () => request.Validate();

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Validate_NonPositiveTimeout_ThrowsArgumentException()
    {
        // Arrange
        var request = new GrpcRequest("Svc", "Method", [])
        {
            TimeoutMilliseconds = 0
        };

        // Act
        var act = () => request.Validate();

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void GetPayloadHash_SamePayload_ReturnsConsistentHash()
    {
        // Arrange
        var payload = new byte[] { 1, 2, 3, 4 };
        var request1 = new GrpcRequest("Svc", "Method", payload);
        var request2 = new GrpcRequest("Svc", "Method", (byte[])payload.Clone());

        // Act
        var hash1 = request1.GetPayloadHash();
        var hash2 = request2.GetPayloadHash();

        // Assert
        hash1.Should().Be(hash2);
        hash1.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void GetPayloadCopy_ReturnsIndependentClone()
    {
        // Arrange
        var payload = new byte[] { 1, 2, 3 };
        var request = new GrpcRequest("Svc", "Method", payload);

        // Act
        var copy = request.GetPayloadCopy();
        copy[0] = 99;

        // Assert
        copy.Should().NotBeSameAs(request.Payload);
        request.Payload[0].Should().Be(1);
    }

    [Fact]
    public void Equals_SameId_ReturnsTrue()
    {
        // Arrange
        var request1 = new GrpcRequest("Svc", "Method", []);
        var request2 = new GrpcRequest("OtherSvc", "OtherMethod", []) { Id = request1.Id };

        // Act & Assert
        request1.Equals(request2).Should().BeTrue();
        request1.GetHashCode().Should().Be(request2.GetHashCode());
    }
}

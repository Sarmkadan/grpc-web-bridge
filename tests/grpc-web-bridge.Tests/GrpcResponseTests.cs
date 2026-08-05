#nullable enable
using FluentAssertions;
using GrpcWebBridge.Domain;
using GrpcWebBridge.Domain.Models;
using Xunit;

namespace GrpcWebBridge.Tests;

/// <summary>
/// Unit tests for GrpcResponse covering construction, success/error transitions,
/// metadata handling, and validation rules.
/// </summary>
public sealed class GrpcResponseTests
{
    [Fact]
    public void Constructor_WithRequestIdAndPayload_SetsExpectedDefaults()
    {
        // Arrange
        var payload = new byte[] { 1, 2, 3 };

        // Act
        var response = new GrpcResponse(" req-1 ", payload);

        // Assert
        response.RequestId.Should().Be("req-1");
        response.Payload.Should().BeEquivalentTo(payload);
        response.Status.Should().Be(GrpcStatusCode.Ok);
        response.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Constructor_WithRequestIdAndPayload_NullPayload_DefaultsToEmptyArray()
    {
        // Act
        var response = new GrpcResponse("req-1", null!);

        // Assert
        response.Payload.Should().NotBeNull();
        response.Payload.Should().BeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Constructor_WithInvalidRequestId_ThrowsArgumentException(string? requestId)
    {
        // Act
        var act = () => new GrpcResponse(requestId!, []);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_WithStatusAndMessage_SetsFields()
    {
        // Act
        var response = new GrpcResponse("req-2", GrpcStatusCode.NotFound, "not found");

        // Assert
        response.RequestId.Should().Be("req-2");
        response.Status.Should().Be(GrpcStatusCode.NotFound);
        response.StatusMessage.Should().Be("not found");
        response.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void SetSuccess_UpdatesStatusPayloadAndFormat()
    {
        // Arrange
        var response = new GrpcResponse("req-3", GrpcStatusCode.Internal, "boom");
        var payload = new byte[] { 9, 9 };

        // Act
        response.SetSuccess(payload, SerializationFormat.Json);

        // Assert
        response.Status.Should().Be(GrpcStatusCode.Ok);
        response.StatusMessage.Should().Be("OK");
        response.Payload.Should().BeEquivalentTo(payload);
        response.PayloadFormat.Should().Be(SerializationFormat.Json);
        response.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void SetError_WithOkStatus_ThrowsArgumentException()
    {
        // Arrange
        var response = new GrpcResponse("req-4", new byte[] { 1 });

        // Act
        var act = () => response.SetError(GrpcStatusCode.Ok, "should fail");

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void SetError_ClearsPayloadAndSetsErrorDetails()
    {
        // Arrange
        var response = new GrpcResponse("req-5", new byte[] { 1, 2, 3 });

        // Act
        response.SetError(GrpcStatusCode.Internal, "failure", "stack trace");

        // Assert
        response.Status.Should().Be(GrpcStatusCode.Internal);
        response.StatusMessage.Should().Be("failure");
        response.ErrorDetails.Should().Be("stack trace");
        response.Payload.Should().BeEmpty();
        response.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void AddMetadata_And_GetMetadata_RoundTrips()
    {
        // Arrange
        var response = new GrpcResponse();

        // Act
        response.AddMetadata("key", "value");

        // Assert
        response.GetMetadata("key").Should().Be("value");
        response.GetMetadata("missing").Should().BeNull();
    }

    [Fact]
    public void AddMetadata_WithEmptyKey_ThrowsArgumentException()
    {
        // Arrange
        var response = new GrpcResponse();

        // Act
        var act = () => response.AddMetadata(" ", "value");

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AddTrailingMetadata_And_GetTrailingMetadata_RoundTrips()
    {
        // Arrange
        var response = new GrpcResponse();

        // Act
        response.AddTrailingMetadata("trace-id", "abc123");

        // Assert
        response.GetTrailingMetadata("trace-id").Should().Be("abc123");
        response.GetTrailingMetadata("missing").Should().BeNull();
    }

    [Fact]
    public void Validate_SuccessResponseWithoutStatusMessage_DoesNotThrow()
    {
        // Arrange
        var response = new GrpcResponse("req-6", new byte[] { 1 })
        {
            DurationMilliseconds = 5
        };

        // Act
        var act = response.Validate;

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_ErrorResponseWithoutStatusMessage_ThrowsArgumentException()
    {
        // Arrange
        var response = new GrpcResponse("req-7", GrpcStatusCode.Internal, null)
        {
            StatusMessage = null
        };

        // Act
        var act = response.Validate;

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Validate_NegativeDuration_ThrowsArgumentException()
    {
        // Arrange
        var response = new GrpcResponse("req-8", new byte[] { 1 })
        {
            DurationMilliseconds = -1
        };

        // Act
        var act = response.Validate;

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void GetPayloadCopy_ReturnsIndependentArray()
    {
        // Arrange
        var response = new GrpcResponse("req-9", new byte[] { 1, 2, 3 });

        // Act
        var copy = response.GetPayloadCopy();
        copy[0] = 99;

        // Assert
        copy.Should().NotBeSameAs(response.Payload);
        response.Payload[0].Should().Be(1);
    }

    [Fact]
    public void GetPayloadHash_EmptyPayload_ReturnsConsistentHash()
    {
        // Arrange
        var response1 = new GrpcResponse("req-10", []);
        var response2 = new GrpcResponse("req-11", []);

        // Act
        var hash1 = response1.GetPayloadHash();
        var hash2 = response2.GetPayloadHash();

        // Assert
        hash1.Should().Be(hash2);
        hash1.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Equals_ComparesById()
    {
        // Arrange
        var response = new GrpcResponse();
        var clone = new GrpcResponse { Id = response.Id };
        var other = new GrpcResponse();

        // Act & Assert
        response.Equals(clone).Should().BeTrue();
        response.Equals(other).Should().BeFalse();
        response.Equals(null).Should().BeFalse();
        response.GetHashCode().Should().Be(clone.GetHashCode());
    }
}

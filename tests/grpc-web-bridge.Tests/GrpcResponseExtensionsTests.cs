#nullable enable

using FluentAssertions;
using GrpcWebBridge.Domain.Models;
using GrpcWebBridge.Domain;
using Xunit;

namespace GrpcWebBridge.Tests;

public sealed class GrpcResponseExtensionsTests
{
    [Fact]
    public void ToSuccessResponse_WithValidParameters_CreatesSuccessResponse()
    {
        // Arrange
        const string requestId = "test-request-123";
        var payload = new byte[] { 0x01, 0x02, 0x03, 0x04 };
        const SerializationFormat format = SerializationFormat.Json;

        // Act
        var response = requestId.ToSuccessResponse(payload, format);

        // Assert
        response.Should().NotBeNull();
        response.RequestId.Should().Be(requestId);
        response.Status.Should().Be(GrpcStatusCode.Ok);
        response.StatusMessage.Should().Be("OK");
        response.Payload.Should().BeEquivalentTo(payload);
        response.PayloadFormat.Should().Be(format);
        response.IsSuccess.Should().BeTrue();
        response.ErrorDetails.Should().BeNull();
        response.Metadata.Should().BeEmpty();
        response.TrailingMetadata.Should().BeEmpty();
    }

    [Fact]
    public void ToSuccessResponse_WithEmptyPayload_CreatesResponseWithEmptyPayload()
    {
        // Arrange
        const string requestId = "test-request-456";
        var emptyPayload = Array.Empty<byte>();

        // Act
        var response = requestId.ToSuccessResponse(emptyPayload);

        // Assert
        response.Should().NotBeNull();
        response.Payload.Should().BeEmpty();
        response.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void ToSuccessResponse_WithLargePayload_CreatesResponseWithPayload()
    {
        // Arrange
        const string requestId = "test-request-large";
        var largePayload = new byte[1024]; // 1KB payload
        new Random().NextBytes(largePayload);

        // Act
        var response = requestId.ToSuccessResponse(largePayload);

        // Assert
        response.Should().NotBeNull();
        response.Payload.Should().BeEquivalentTo(largePayload);
        response.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void ToSuccessResponse_NullRequestId_ThrowsArgumentNullException()
    {
        // Arrange
        string? requestId = null;
        var payload = new byte[] { 0x01, 0x02 };

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => requestId!.ToSuccessResponse(payload));
    }

    [Fact]
    public void ToSuccessResponse_NullPayload_ThrowsArgumentNullException()
    {
        // Arrange
        const string requestId = "test-request-null-payload";
        byte[]? payload = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => requestId.ToSuccessResponse(payload!));
    }

    [Fact]
    public void ToSuccessResponse_DefaultFormat_UsesProtobufFormat()
    {
        // Arrange
        const string requestId = "test-request-default";
        var payload = new byte[] { 0xFF, 0xFE };

        // Act
        var response = requestId.ToSuccessResponse(payload);

        // Assert
        response.Should().NotBeNull();
        response.PayloadFormat.Should().Be(SerializationFormat.Protobuf);
    }

    [Fact]
    public void ToErrorResponse_WithValidErrorParameters_CreatesErrorResponse()
    {
        // Arrange
        const string requestId = "error-request-123";
        const GrpcStatusCode statusCode = GrpcStatusCode.InvalidArgument;
        const string message = "Invalid request parameters";
        const string details = "Field 'name' is required";

        // Act
        var response = requestId.ToErrorResponse(statusCode, message, details);

        // Assert
        response.Should().NotBeNull();
        response.RequestId.Should().Be(requestId);
        response.Status.Should().Be(statusCode);
        response.StatusMessage.Should().Be(message);
        response.ErrorDetails.Should().Be(details);
        response.Payload.Should().BeEmpty();
        response.IsSuccess.Should().BeFalse();
        response.Metadata.Should().BeEmpty();
        response.TrailingMetadata.Should().BeEmpty();
    }

    [Fact]
    public void ToErrorResponse_WithDifferentStatusCodes_CreatesResponsesWithCorrectStatuses()
    {
        // Arrange
        const string requestId = "multi-error-request";
        var statusCodes = new GrpcStatusCode[]
        {
            GrpcStatusCode.NotFound,
            GrpcStatusCode.PermissionDenied,
            GrpcStatusCode.Internal,
            GrpcStatusCode.Unavailable
        };

        // Act & Assert
        foreach (var statusCode in statusCodes)
        {
            var response = requestId.ToErrorResponse(statusCode, $"Error for {statusCode}");
            response.Status.Should().Be(statusCode);
            response.IsSuccess.Should().BeFalse();
        }
    }

    [Fact]
    public void ToErrorResponse_NullRequestId_ThrowsArgumentNullException()
    {
        // Arrange
        string? requestId = null;
        const GrpcStatusCode statusCode = GrpcStatusCode.FailedPrecondition;
        const string message = "Precondition failed";

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => requestId!.ToErrorResponse(statusCode, message));
    }

    [Fact]
    public void ToErrorResponse_NullMessage_ThrowsArgumentNullException()
    {
        // Arrange
        const string requestId = "error-request-null-message";
        const GrpcStatusCode statusCode = GrpcStatusCode.Unknown;
        string? message = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => requestId.ToErrorResponse(statusCode, message!));
    }

    [Fact]
    public void ToErrorResponse_OkStatusCode_ThrowsArgumentException()
    {
        // Arrange
        const string requestId = "ok-status-error";
        const GrpcStatusCode statusCode = GrpcStatusCode.Ok;
        const string message = "This should fail";

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(
            () => requestId.ToErrorResponse(statusCode, message));
        exception.Message.Should().Contain("Cannot create error response with Ok status");
    }

    [Fact]
    public void ToErrorResponse_EmptyDetails_CreatesResponseWithoutDetails()
    {
        // Arrange
        const string requestId = "error-request-no-details";
        const GrpcStatusCode statusCode = GrpcStatusCode.DeadlineExceeded;
        const string message = "Request timed out";

        // Act
        var response = requestId.ToErrorResponse(statusCode, message);

        // Assert
        response.Should().NotBeNull();
        response.ErrorDetails.Should().BeNull();
    }

    [Fact]
    public void AddMetadata_WithValidDictionary_AddsAllMetadataEntries()
    {
        // Arrange
        var response = new GrpcResponse("metadata-test", GrpcStatusCode.Ok, "OK");
        var metadata = new Dictionary<string, string>
        {
            { "X-Custom-Header", "value1" },
            { "Authorization", "Bearer token123" },
            { "Content-Type", "application/json" }
        };

        // Act
        response.AddMetadata(metadata);

        // Assert
        response.Metadata.Should().HaveCount(3);
        response.Metadata["X-Custom-Header"].Should().Be("value1");
        response.Metadata["Authorization"].Should().Be("Bearer token123");
        response.Metadata["Content-Type"].Should().Be("application/json");
    }

    [Fact]
    public void AddMetadata_WithEmptyDictionary_DoesNotModifyMetadata()
    {
        // Arrange
        var response = new GrpcResponse("empty-metadata-test", GrpcStatusCode.Ok, "OK");
        var emptyMetadata = new Dictionary<string, string>();

        // Act
        response.AddMetadata(emptyMetadata);

        // Assert
        response.Metadata.Should().BeEmpty();
    }

    [Fact]
    public void AddMetadata_NullResponse_ThrowsArgumentNullException()
    {
        // Arrange
        GrpcResponse? response = null;
        var metadata = new Dictionary<string, string> { { "key", "value" } };

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => response!.AddMetadata(metadata));
    }

    [Fact]
    public void AddMetadata_NullDictionary_ThrowsArgumentNullException()
    {
        // Arrange
        var response = new GrpcResponse("null-dict-test", GrpcStatusCode.Ok, "OK");
        Dictionary<string, string>? metadata = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => response.AddMetadata(metadata!));
    }

    [Fact]
    public void AddMetadata_WithWhitespaceKey_DoesNotAddEntry()
    {
        // Arrange
        var response = new GrpcResponse("whitespace-key-test", GrpcStatusCode.Ok, "OK");
        var metadata = new Dictionary<string, string>
        {
            { "valid-key", "valid-value" },
            { "  ", "ignored-value" },
            { "", "empty-key-value" }
        };

        // Act
        response.AddMetadata(metadata);

        // Assert
        response.Metadata.Should().HaveCount(1);
        response.Metadata.Should().ContainKey("valid-key");
        response.Metadata.Should().NotContainKey("  ");
        response.Metadata.Should().NotContainKey("");
    }

    [Fact]
    public void AddMetadata_OverwritesExistingKey_UpdatesValue()
    {
        // Arrange
        var response = new GrpcResponse("overwrite-test", GrpcStatusCode.Ok, "OK");
        response.AddMetadata(new Dictionary<string, string> { { "key", "original" } });
        var newMetadata = new Dictionary<string, string> { { "key", "updated" } };

        // Act
        response.AddMetadata(newMetadata);

        // Assert
        response.Metadata["key"].Should().Be("updated");
    }

    [Fact]
    public void AddTrailingMetadata_WithValidDictionary_AddsAllTrailingMetadataEntries()
    {
        // Arrange
        var response = new GrpcResponse("trailing-metadata-test", GrpcStatusCode.Ok, "OK");
        var trailingMetadata = new Dictionary<string, string>
        {
            { "X-Trailing-Header", "trailing-value1" },
            { "X-Request-ID", "req-789" },
            { "X-Correlation-ID", "corr-456" }
        };

        // Act
        response.AddTrailingMetadata(trailingMetadata);

        // Assert
        response.TrailingMetadata.Should().HaveCount(3);
        response.TrailingMetadata["X-Trailing-Header"].Should().Be("trailing-value1");
        response.TrailingMetadata["X-Request-ID"].Should().Be("req-789");
        response.TrailingMetadata["X-Correlation-ID"].Should().Be("corr-456");
    }

    [Fact]
    public void AddTrailingMetadata_WithEmptyDictionary_DoesNotModifyTrailingMetadata()
    {
        // Arrange
        var response = new GrpcResponse("empty-trailing-test", GrpcStatusCode.Ok, "OK");
        var emptyTrailingMetadata = new Dictionary<string, string>();

        // Act
        response.AddTrailingMetadata(emptyTrailingMetadata);

        // Assert
        response.TrailingMetadata.Should().BeEmpty();
    }

    [Fact]
    public void AddTrailingMetadata_NullResponse_ThrowsArgumentNullException()
    {
        // Arrange
        GrpcResponse? response = null;
        var trailingMetadata = new Dictionary<string, string> { { "key", "value" } };

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => response!.AddTrailingMetadata(trailingMetadata));
    }

    [Fact]
    public void AddTrailingMetadata_NullDictionary_ThrowsArgumentNullException()
    {
        // Arrange
        var response = new GrpcResponse("null-trailing-dict-test", GrpcStatusCode.Ok, "OK");
        Dictionary<string, string>? trailingMetadata = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => response.AddTrailingMetadata(trailingMetadata!));
    }

    [Fact]
    public void AddTrailingMetadata_WithWhitespaceKey_DoesNotAddEntry()
    {
        // Arrange
        var response = new GrpcResponse("whitespace-trailing-key-test", GrpcStatusCode.Ok, "OK");
        var trailingMetadata = new Dictionary<string, string>
        {
            { "valid-key", "valid-value" },
            { "  ", "ignored-value" },
            { "", "empty-key-value" }
        };

        // Act
        response.AddTrailingMetadata(trailingMetadata);

        // Assert
        response.TrailingMetadata.Should().HaveCount(1);
        response.TrailingMetadata.Should().ContainKey("valid-key");
        response.TrailingMetadata.Should().NotContainKey("  ");
        response.TrailingMetadata.Should().NotContainKey("");
    }

    [Fact]
    public void AddTrailingMetadata_OverwritesExistingKey_UpdatesValue()
    {
        // Arrange
        var response = new GrpcResponse("trailing-overwrite-test", GrpcStatusCode.Ok, "OK");
        response.AddTrailingMetadata(new Dictionary<string, string> { { "key", "original" } });
        var newTrailingMetadata = new Dictionary<string, string> { { "key", "updated" } };

        // Act
        response.AddTrailingMetadata(newTrailingMetadata);

        // Assert
        response.TrailingMetadata["key"].Should().Be("updated");
    }

    [Fact]
    public void Integration_ToSuccessResponseThenAddMetadata_CreatesCompleteResponse()
    {
        // Arrange
        const string requestId = "integration-test-123";
        var payload = new byte[] { 0xAA, 0xBB, 0xCC };
        var metadata = new Dictionary<string, string>
        {
            { "X-API-Version", "1.0" },
            { "X-Environment", "production" }
        };
        var trailingMetadata = new Dictionary<string, string>
        {
            { "X-Request-ID", requestId },
            { "X-Timestamp", DateTime.UtcNow.ToString("O") }
        };

        // Act
        var response = requestId.ToSuccessResponse(payload);
        response.AddMetadata(metadata);
        response.AddTrailingMetadata(trailingMetadata);

        // Assert
        response.Should().NotBeNull();
        response.IsSuccess.Should().BeTrue();
        response.Payload.Should().BeEquivalentTo(payload);
        response.Metadata.Should().HaveCount(2);
        response.TrailingMetadata.Should().HaveCount(2);
        response.Metadata["X-API-Version"].Should().Be("1.0");
        response.TrailingMetadata["X-Request-ID"].Should().Be(requestId);
    }

    [Fact]
    public void Integration_ToErrorResponseThenAddMetadata_CreatesCompleteErrorResponse()
    {
        // Arrange
        const string requestId = "error-integration-test";
        const GrpcStatusCode statusCode = GrpcStatusCode.ResourceExhausted;
        const string message = "Rate limit exceeded";
        const string details = "Daily quota of 1000 requests exceeded";
        var metadata = new Dictionary<string, string> { { "Retry-After", "3600" } };

        // Act
        var response = requestId.ToErrorResponse(statusCode, message, details);
        response.AddMetadata(metadata);

        // Assert
        response.Should().NotBeNull();
        response.IsSuccess.Should().BeFalse();
        response.Status.Should().Be(statusCode);
        response.ErrorDetails.Should().Be(details);
        response.Metadata.Should().HaveCount(1);
        response.Metadata["Retry-After"].Should().Be("3600");
    }
}
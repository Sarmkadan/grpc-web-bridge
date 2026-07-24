#nullable enable

using FluentAssertions;
using GrpcWebBridge.Domain;
using GrpcWebBridge.Domain.Models;
using Xunit;

namespace GrpcWebBridge.Tests;

public sealed class GrpcRequestExtensionsTests
{
    private static GrpcRequest CreateTestRequest()
    {
        var request = new GrpcRequest("TestService", "TestMethod", [1, 2, 3, 4, 5])
        {
            PayloadFormat = SerializationFormat.Json,
            MethodType = MethodType.Unary,
            TimeoutMilliseconds = 5000
        };
        request.AddMetadata("test-key", "test-value");
        request.AddMetadata("authorization", "Bearer token123");
        return request;
    }

    [Fact]
    public void HasMetadataKey_NullRequest_ThrowsArgumentNullException()
    {
        // Arrange
        GrpcRequest? request = null;
        const string key = "test-key";

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => request!.HasMetadataKey(key));
    }

    [Fact]
    public void HasMetadataKey_ExistingKey_ReturnsTrue()
    {
        // Arrange
        var request = CreateTestRequest();
        const string key = "test-key";

        // Act
        var result = request.HasMetadataKey(key);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void HasMetadataKey_NonExistingKey_ReturnsFalse()
    {
        // Arrange
        var request = CreateTestRequest();
        const string key = "non-existing-key";

        // Act
        var result = request.HasMetadataKey(key);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void GetMetadataValue_ExistingKey_ReturnsValue()
    {
        // Arrange
        var request = CreateTestRequest();
        const string key = "test-key";
        const string expectedValue = "test-value";

        // Act
        var result = request.GetMetadataValue(key);

        // Assert
        result.Should().Be(expectedValue);
    }

    [Fact]
    public void GetMetadataValue_NonExistingKey_ReturnsNull()
    {
        // Arrange
        var request = CreateTestRequest();
        const string key = "non-existing-key";

        // Act
        var result = request.GetMetadataValue(key);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetMetadataValue_WithDefaultValue_NonExistingKey_ReturnsDefault()
    {
        // Arrange
        var request = CreateTestRequest();
        const string key = "non-existing-key";
        const string defaultValue = "default-value";

        // Act
        var result = request.GetMetadataValue(key, defaultValue);

        // Assert
        result.Should().Be(defaultValue);
    }

    [Fact]
    public void GetMetadataValue_IntTypeConversion_ReturnsIntValue()
    {
        // Arrange
        var request = CreateTestRequest();
        request.AddMetadata("timeout", "30000");
        const string key = "timeout";

        // Act
        var result = request.GetMetadataValue<int>(key);

        // Assert
        result.Should().Be(30000);
    }

    [Fact]
    public void ToLogString_WithoutMetadata_ReturnsBasicLogString()
    {
        // Arrange
        var request = CreateTestRequest();

        // Act
        var result = request.ToLogString();

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().Contain(request.Id);
        result.Should().Contain(request.FullMethodName);
    }

    [Fact]
    public void GetPayloadSize_WithPayload_ReturnsCorrectSize()
    {
        // Arrange
        var payload = new byte[] { 1, 2, 3, 4, 5 };
        var request = new GrpcRequest("TestService", "TestMethod", payload);

        // Act
        var result = request.GetPayloadSize();

        // Assert
        result.Should().Be(5);
    }

    [Fact]
    public void GetPayloadHashHex_WithPayload_ReturnsValidHash()
    {
        // Arrange
        var payload = new byte[] { 1, 2, 3, 4, 5 };
        var request = new GrpcRequest("TestService", "TestMethod", payload);

        // Act
        var result = request.GetPayloadHashHex();

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().HaveLength(64);
        result.Should().MatchRegex("^[0-9A-Fa-f]+$");
    }

    [Fact]
    public void IsPayloadEmpty_EmptyPayload_ReturnsTrue()
    {
        // Arrange
        var request = new GrpcRequest("TestService", "TestMethod", []);

        // Act
        var result = request.IsPayloadEmpty();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsPayloadEmpty_WithPayload_ReturnsFalse()
    {
        // Arrange
        var payload = new byte[] { 1, 2, 3 };
        var request = new GrpcRequest("TestService", "TestMethod", payload);

        // Act
        var result = request.IsPayloadEmpty();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void HasMetadataKey_EmptyMetadataDictionary_ReturnsFalse()
    {
        // Arrange
        var request = new GrpcRequest("TestService", "TestMethod", []);

        // Act
        var result = request.HasMetadataKey("any-key");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void GetMetadataValue_NullDefaultValue_ReturnsNull()
    {
        // Arrange
        var request = new GrpcRequest("TestService", "TestMethod", []);
        const string key = "non-existent";

        // Act
        var result = request.GetMetadataValue(key, null);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetMetadataValueGeneric_DoubleTypeConversion_ReturnsDoubleValue()
    {
        // Arrange
        var request = CreateTestRequest();
        request.AddMetadata("rate", "2.71828");
        const string key = "rate";

        // Act
        var result = request.GetMetadataValue<double>(key);

        // Assert
        result.Should().BeApproximately(2.71828, 0.00001);
    }

    [Fact]
    public void GetMetadataValueGeneric_BoolTypeConversion_ReturnsTrue()
    {
        // Arrange
        var request = CreateTestRequest();
        request.AddMetadata("enabled", "true");
        const string key = "enabled";

        // Act
        var result = request.GetMetadataValue<bool>(key);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void GetMetadataValueGeneric_BoolTypeConversion_ReturnsFalse()
    {
        // Arrange
        var request = CreateTestRequest();
        request.AddMetadata("disabled", "false");
        const string key = "disabled";

        // Act
        var result = request.GetMetadataValue<bool>(key);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void GetMetadataValueGeneric_LongTypeConversion_ReturnsLongValue()
    {
        // Arrange
        var request = CreateTestRequest();
        request.AddMetadata("timestamp", "1234567890");
        const string key = "timestamp";

        // Act
        var result = request.GetMetadataValue<long>(key);

        // Assert
        result.Should().Be(1234567890);
    }

    [Fact]
    public void GetMetadataValueGeneric_DateTimeTypeConversion_ReturnsDateTimeValue()
    {
        // Arrange
        var request = CreateTestRequest();
        request.AddMetadata("date", "2024-01-15T10:30:00Z");
        const string key = "date";

        // Act
        var result = request.GetMetadataValue<DateTime>(key);

        // Assert
        result.Should().Be(new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void GetMetadataValueGeneric_StringType_ReturnsOriginalString()
    {
        // Arrange
        var request = CreateTestRequest();
        request.AddMetadata("username", "john.doe");
        const string key = "username";

        // Act
        var result = request.GetMetadataValue<string>(key);

        // Assert
        result.Should().Be("john.doe");
    }

    [Fact]
    public void ToLogString_WithMetadata_IncludesMetadataInOutput()
    {
        // Arrange
        var request = CreateTestRequest();
        request.AddMetadata("authorization", "Bearer secret-token");
        request.AddMetadata("user-id", "user123");

        // Act
        var result = request.ToLogString(includeMetadata: true);

        // Assert
        result.Should().Contain("Metadata:");
        result.Should().Contain("authorization=Bearer secret-token");
        result.Should().Contain("user-id=user123");
    }

    [Fact]
    public void ToLogString_MaxMetadataLengthZero_TruncatesCompletely()
    {
        // Arrange
        var request = CreateTestRequest();
        request.AddMetadata("key1", "value1");
        request.AddMetadata("key2", "value2");

        // Act
        var result = request.ToLogString(includeMetadata: true, maxMetadataLength: 0);

        // Assert
        result.Should().Contain("...(truncated)");
        result.Should().NotContain("key1");
        result.Should().NotContain("key2");
    }

    [Fact]
    public void GetPayloadSize_EmptyPayload_ReturnsZero()
    {
        // Arrange
        var request = new GrpcRequest("TestService", "TestMethod", []);

        // Act
        var result = request.GetPayloadSize();

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public void GetPayloadSize_LargePayload_ReturnsCorrectSize()
    {
        // Arrange
        var largePayload = new byte[1024 * 1024]; // 1MB
        var request = new GrpcRequest("TestService", "TestMethod", largePayload);

        // Act
        var result = request.GetPayloadSize();

        // Assert
        result.Should().Be(1024 * 1024);
    }

    [Fact]
    public void GetPayloadHashHex_DifferentPayloads_ReturnDifferentHashes()
    {
        // Arrange
        var payload1 = new byte[] { 1, 2, 3, 4, 5 };
        var payload2 = new byte[] { 1, 2, 3, 4, 6 };
        var request1 = new GrpcRequest("TestService", "TestMethod", payload1);
        var request2 = new GrpcRequest("TestService", "TestMethod", payload2);

        // Act
        var hash1 = request1.GetPayloadHashHex();
        var hash2 = request2.GetPayloadHashHex();

        // Assert
        hash1.Should().NotBe(hash2);
    }

    [Fact]
    public void GetPayloadHashHex_EmptyPayload_ReturnsConsistentHash()
    {
        // Arrange
        var request1 = new GrpcRequest("TestService", "TestMethod", []);
        var request2 = new GrpcRequest("TestService", "TestMethod", []);

        // Act
        var hash1 = request1.GetPayloadHashHex();
        var hash2 = request2.GetPayloadHashHex();

        // Assert
        hash1.Should().Be(hash2);
    }

    [Fact]
    public void IsPayloadEmpty_NullPayload_ReturnsTrue()
    {
        // Arrange
        var request = new GrpcRequest("TestService", "TestMethod", null!);

        // Act
        var result = request.IsPayloadEmpty();

        // Assert
        result.Should().BeTrue();
    }

}
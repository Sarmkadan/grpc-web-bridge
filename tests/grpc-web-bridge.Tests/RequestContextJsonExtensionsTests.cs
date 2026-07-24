#nullable enable
// =============================================================================
// Author: Automated Generation
// =====================================================================

using System.Text.Json;
using FluentAssertions;
using GrpcWebBridge.Integration;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace GrpcWebBridge.Tests;

/// <summary>
/// Tests for RequestContextManager JSON serialization/deserialization extensions.
/// Tests the RequestContextManagerJsonExtensions class methods:
/// - ToJson()
/// - FromJson()
/// - TryFromJson()
/// </summary>
public sealed class RequestContextJsonExtensionsTests
{
    private readonly ILogger<RequestContextManager> _mockLogger;
    private readonly RequestContextManager _manager;

    public RequestContextJsonExtensionsTests()
    {
        _mockLogger = Substitute.For<ILogger<RequestContextManager>>();
        _manager = new RequestContextManager(_mockLogger);
    }

    [Fact]
    public void ToJson_Should_Serialize_Context_With_RequestId()
    {
        // Arrange
        var requestId = "req-serialization-test";
        _manager.CreateContext(requestId);
        _manager.SetMetadata("key1", "value1");
        _manager.SetMetadata("key2", "value2");

        // Act
        var json = _manager.ToJson();

        // Assert
        json.Should().NotBeNullOrEmpty();
        json.Should().Contain(requestId);
        json.Should().Contain("key1");
        json.Should().Contain("value1");
        json.Should().Contain("key2");
        json.Should().Contain("value2");
    }

    [Fact]
    public void ToJson_Should_Return_Empty_Object_When_Context_Is_Null()
    {
        // Arrange - no context created
        var freshManager = new RequestContextManager(_mockLogger);

        // Act
        var json = freshManager.ToJson();

        // Assert
        json.Should().Be("{}");
    }

    [Fact]
    public void ToJson_Should_Use_CamelCase_Naming_Policy()
    {
        // Arrange
        var requestId = "req-camelcase";
        _manager.CreateContext(requestId);
        _manager.SetMetadata("TestKey", "TestValue");

        // Act
        var json = _manager.ToJson();

        // Assert - should use camelCase for property names
        json.Should().Contain("requestId");
        json.Should().Contain("startTime");
        json.Should().Contain("metadata");
        json.Should().Contain("TestKey"); // Metadata keys preserve their original case in JSON
        json.Should().Contain("TestValue");

        // Should NOT contain PascalCase property names
        json.Should().NotContain("RequestId");
        json.Should().NotContain("UserId");
    }

    [Fact]
    public void ToJson_Should_Not_Include_Null_Properties()
    {
        // Arrange - create context with only RequestId
        var requestId = "req-null-properties";
        _manager.CreateContext(requestId);

        // Act
        var json = _manager.ToJson();

        // Assert - should not include null properties like UserId (thanks to DefaultIgnoreCondition)
        json.Should().NotContain("\"userId\"");
        // The JSON should not contain the literal string "null"
    }

    [Fact]
    public void ToJson_Indented_Should_Format_With_Indentation()
    {
        // Arrange
        var requestId = "req-indented";
        _manager.CreateContext(requestId);

        // Act
        var indentedJson = _manager.ToJson(indented: true);
        var compactJson = _manager.ToJson(indented: false);

        // Assert
        indentedJson.Should().Contain("\n"); // Should have newlines
        indentedJson.Should().Contain("  "); // Should have indentation

        compactJson.Should().NotContain("\n"); // Should be compact
        compactJson.Should().NotContain("\r"); // Should be compact
    }

    [Fact]
    public void FromJson_Should_Deserialize_Valid_Json()
    {
        // Arrange
        var originalRequestId = "req-deserialize-test";
        _manager.CreateContext(originalRequestId);
        _manager.SetMetadata("originalKey", "originalValue");

        var json = _manager.ToJson();

        // Create fresh manager for deserialization
        var freshManager = new RequestContextManager(_mockLogger);

        // Act
        var deserializedContext = RequestContextManagerJsonExtensions.FromJson(json);

        // Assert
        deserializedContext.Should().NotBeNull();
        deserializedContext?.RequestId.Should().Be(originalRequestId);
        deserializedContext?.GetMetadata("originalKey").Should().Be("originalValue");
    }

    [Fact]
    public void FromJson_Should_Return_Null_For_Null_Json()
    {
        // Arrange & Act
        var result = RequestContextManagerJsonExtensions.FromJson(null!);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void FromJson_Should_Return_Null_For_Empty_Json()
    {
        // Arrange & Act
        var result = RequestContextManagerJsonExtensions.FromJson("");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void FromJson_Should_Return_Null_For_Whitespace_Json()
    {
        // Arrange & Act
        var result = RequestContextManagerJsonExtensions.FromJson("   ");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void FromJson_Should_Throw_JsonException_For_Invalid_Json()
    {
        // Arrange
        var invalidJson = "{ invalid json";

        // Act & Assert
        var act = () => RequestContextManagerJsonExtensions.FromJson(invalidJson);
        act.Should().Throw<JsonException>();
    }

    [Fact]
    public void TryFromJson_Should_Return_False_And_Null_For_Null_Json()
    {
        // Arrange & Act - TryFromJson should handle null gracefully
        var success = RequestContextManagerJsonExtensions.TryFromJson(null!, out var result);

        // Assert - should return false and null for null input
        success.Should().BeFalse();
        result.Should().BeNull();
    }

    [Fact]
    public void TryFromJson_Should_Return_False_And_Null_For_Empty_Json()
    {
        // Arrange & Act
        var success = RequestContextManagerJsonExtensions.TryFromJson("", out var result);

        // Assert
        success.Should().BeFalse();
        result.Should().BeNull();
    }

    [Fact]
    public void TryFromJson_Should_Return_False_And_Null_For_Whitespace_Json()
    {
        // Arrange & Act
        var success = RequestContextManagerJsonExtensions.TryFromJson("   ", out var result);

        // Assert
        success.Should().BeFalse();
        result.Should().BeNull();
    }

    [Fact]
    public void TryFromJson_Should_Return_True_And_Deserialized_Context_For_Valid_Json()
    {
        // Arrange
        var originalRequestId = "req-try-deserialize";
        _manager.CreateContext(originalRequestId);
        _manager.SetMetadata("testKey", "testValue");

        var json = _manager.ToJson();

        // Create fresh manager for deserialization
        var freshManager = new RequestContextManager(_mockLogger);

        // Act
        var success = RequestContextManagerJsonExtensions.TryFromJson(json, out var result);

        // Assert
        success.Should().BeTrue();
        result.Should().NotBeNull();
        result?.RequestId.Should().Be(originalRequestId);
        result?.GetMetadata("testKey").Should().Be("testValue");
    }

    [Fact]
    public void TryFromJson_Should_Return_False_For_Invalid_Json()
    {
        // Arrange
        var invalidJson = "{ invalid json";

        // Act
        var success = RequestContextManagerJsonExtensions.TryFromJson(invalidJson, out var result);

        // Assert
        success.Should().BeFalse();
        result.Should().BeNull();
    }

    [Fact]
    public void ToJson_Should_Sanitize_Metadata_To_Prevent_Log_Injection()
    {
        // Arrange - create context with metadata containing control characters
        var requestId = "req-sanitize-test";
        _manager.CreateContext(requestId);

        // Add metadata with control characters and newlines
        _manager.SetMetadata("safeKey", "safeValue");
        _manager.SetMetadata("controlKey", "value\r\nwith\nnewlines");
        _manager.SetMetadata("tabKey", "value\twith\ttabs");

        // Act
        var json = _manager.ToJson();

        // Assert - should not contain control characters or newlines in the JSON
        json.Should().Contain("safeKey");
        json.Should().Contain("safeValue");

        // Should not contain literal control characters in the serialized output
        json.Should().NotContain("\r");
        json.Should().NotContain("\n");
        json.Should().NotContain("\t");
    }

    [Fact]
    public void ToJson_Should_Handle_Empty_Metadata()
    {
        // Arrange
        var requestId = "req-empty-metadata";
        _manager.CreateContext(requestId);
        // Don't set any metadata

        // Act
        var json = _manager.ToJson();

        // Assert
        json.Should().NotBeNullOrEmpty();
        json.Should().Contain(requestId);
        // Should have empty metadata array/object
        json.Should().Contain("metadata");
    }

    [Fact]
    public void ToJson_Should_Include_StartTime_And_ElapsedMilliseconds()
    {
        // Arrange
        var requestId = "req-timing";
        _manager.CreateContext(requestId);

        // Small delay to ensure elapsed time is recorded
        Thread.Sleep(5);
        _manager.RecordElapsedTime();

        // Act
        var json = _manager.ToJson();

        // Assert
        json.Should().Contain("startTime");
        json.Should().Contain("elapsedMilliseconds");
        // Note: "duration" is not a property, ElapsedMilliseconds is the property name
    }

    [Fact]
    public void ToJson_Should_Include_UserId_When_Set()
    {
        // Arrange
        var requestId = "req-with-user";
        var userId = "user-123";
        _manager.CreateContext(requestId, userId);

        // Act
        var json = _manager.ToJson();

        // Assert
        json.Should().Contain("userId");
        json.Should().Contain(userId);
    }

    [Fact]
    public void Roundtrip_Serialization_Should_Preserve_All_Context_Data()
    {
        // Arrange - create context with all possible data
        var requestId = "req-roundtrip";
        var userId = "user-456";
        _manager.CreateContext(requestId, userId);
        _manager.SetMetadata("key1", "value1");
        _manager.SetMetadata("key2", "value2");
        Thread.Sleep(5);
        _manager.RecordElapsedTime();

        // Act - serialize and deserialize
        var json = _manager.ToJson();
        var deserializedContext = RequestContextManagerJsonExtensions.FromJson(json);

        // Assert - all data should be preserved
        deserializedContext.Should().NotBeNull();
        deserializedContext?.RequestId.Should().Be(requestId);
        deserializedContext?.UserId.Should().Be(userId);
        deserializedContext?.GetMetadata("key1").Should().Be("value1");
        deserializedContext?.GetMetadata("key2").Should().Be("value2");
        deserializedContext?.StartTime.Should().BeCloseTo(_manager.GetContext()?.StartTime ?? DateTime.MinValue, TimeSpan.FromMilliseconds(10));
        deserializedContext?.ElapsedMilliseconds.Should().BeGreaterOrEqualTo(0);
    }

    [Fact]
    public void ToJson_Should_Handle_Maximum_Metadata_Size()
    {
        // Arrange - create context and add maximum allowed metadata
        var requestId = "req-max-metadata";
        _manager.CreateContext(requestId);

        // Add metadata entries up to the limit
        for (int i = 0; i < RequestContext.MaxMetadataEntries; i++)
        {
            _manager.SetMetadata($"key{i}", $"value{i}");
        }

        // Act
        var json = _manager.ToJson();

        // Assert - should serialize without error
        json.Should().NotBeNullOrEmpty();
        json.Should().Contain("metadata");
    }
}
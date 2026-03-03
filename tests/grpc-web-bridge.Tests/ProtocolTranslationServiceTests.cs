#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using FluentAssertions;
using GrpcWebBridge.Domain;
using GrpcWebBridge.Domain.Models;
using GrpcWebBridge.Services;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace GrpcWebBridge.Tests;

/// <summary>
/// Contains unit tests for <see cref="ProtocolTranslationService"/> ensuring that
/// HTTP‑to‑gRPC translation, protobuf/JSON conversion, metadata handling, and error
/// response creation behave as expected.
/// </summary>
public sealed class ProtocolTranslationServiceTests
{
    private readonly ILogger<ProtocolTranslationService> _logger;
    private readonly ProtocolTranslationService _service;

    /// <summary>
    /// Initializes a new instance of <see cref="ProtocolTranslationServiceTests"/>.
    /// Sets up a mock <see cref="ILogger{ProtocolTranslationService}"/> and creates the
    /// <see cref="ProtocolTranslationService"/> instance under test.
    /// </summary>
    public ProtocolTranslationServiceTests()
    {
        _logger = Substitute.For<ILogger<ProtocolTranslationService>>();
        _service = new ProtocolTranslationService(_logger);
    }

    /// <summary>
    /// Verifies that translating a valid HTTP request to a gRPC request produces a
    /// <see cref="GrpcRequest"/> with the expected service name, method name,
    /// payload, and payload format.
    /// </summary>
    /// <returns>Nothing.</returns>
    [Fact]
    public void TranslateHttpToGrpc_WithValidInput_ReturnsGrpcRequest()
    {
        // Arrange
        var payload = "{}".AsBytes();

        // Act
        var request = _service.TranslateHttpToGrpc("TestService", "TestMethod", payload, SerializationFormat.Json);

        // Assert
        request.Should().NotBeNull();
        request.ServiceName.Should().Be("TestService");
        request.MethodName.Should().Be("TestMethod");
        request.Payload.Should().BeEquivalentTo(payload);
        request.PayloadFormat.Should().Be(SerializationFormat.Json);
    }

    /// <summary>
    /// Ensures that converting an empty protobuf byte array to JSON yields an empty JSON
    /// object (i.e., <c>{}</c>).
    /// </summary>
    /// <returns>Nothing.</returns>
    [Fact]
    public void ConvertProtobufToJson_WithEmptyArray_ReturnsEmptyJson()
    {
        // Arrange
        byte[] protobuf = [];

        // Act
        var json = _service.ConvertProtobufToJson(protobuf);

        // Assert
        System.Text.Encoding.UTF8.GetString(json).Should().Be("{}");
    }

    /// <summary>
    /// Ensures that converting an empty JSON byte array to protobuf returns an empty
    /// protobuf byte array.
    /// </summary>
    /// <returns>Nothing.</returns>
    [Fact]
    public void ConvertJsonToProtobuf_WithEmptyArray_ReturnsEmptyArray()
    {
        // Arrange
        byte[] json = [];

        // Act
        var protobuf = _service.ConvertJsonToProtobuf(json);

        // Assert
        protobuf.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that translating a <c>null</c> metadata dictionary results in an empty,
    /// non‑null dictionary.
    /// </summary>
    /// <returns>Nothing.</returns>
    [Fact]
    public void TranslateMetadata_WithNullMetadata_ReturnsEmptyDictionary()
    {
        // Arrange & Act
        var translated = _service.TranslateMetadata(null!);

        // Assert
        translated.Should().BeEmpty();
        translated.Should().NotBeNull();
    }

    /// <summary>
    /// Confirms that metadata keys are lower‑cased during translation while preserving
    /// their associated values.
    /// </summary>
    /// <returns>Nothing.</returns>
    [Fact]
    public void TranslateMetadata_WithMixedCaseKeys_ReturnsLowercasedKeys()
    {
        // Arrange
        var meta = new Dictionary<string, string>
        {
            ["Auth-Token"] = "abc",
            ["Content-Type"] = "application/grpc"
        };

        // Act
        var translated = _service.TranslateMetadata(meta);

        // Assert
        translated.Should().ContainKey("auth-token").WhoseValue.Should().Be("abc");
        translated.Should().ContainKey("content-type").WhoseValue.Should().Be("application/grpc");
    }

    /// <summary>
    /// Checks that the <c>grpc-timeout</c> header is removed during metadata translation,
    /// while other headers remain unchanged.
    /// </summary>
    /// <returns>Nothing.</returns>
    [Fact]
    public void TranslateMetadata_WithGrpcTimeout_RemovesTimeoutHeader()
    {
        // Arrange
        var meta = new Dictionary<string, string>
        {
            ["grpc-timeout"] = "1000m",
            ["custom-header"] = "value"
        };

        // Act
        var translated = _service.TranslateMetadata(meta);

        // Assert
        translated.Should().NotContainKey("grpc-timeout");
        translated.Should().ContainKey("custom-header").WhoseValue.Should().Be("value");
    }

    /// <summary>
    /// Validates that creating an error response produces a <see cref="GrpcResponse"/>
    /// containing the supplied request identifier, status code, and status message.
    /// </summary>
    /// <returns>Nothing.</returns>
    [Fact]
    public void CreateErrorResponse_WithValidInput_ReturnsGrpcResponseWithError()
    {
        // Arrange
        var requestId = Guid.NewGuid().ToString();

        // Act
        var response = _service.CreateErrorResponse(requestId, GrpcStatusCode.NotFound, "Service not found");

        // Assert
        response.RequestId.Should().Be(requestId);
        response.Status.Should().Be(GrpcStatusCode.NotFound);
        response.StatusMessage.Should().Be("Service not found");
    }

    /// <summary>
    /// Tests the <c>AsBytes</c> extension method, confirming that a string is correctly
    /// encoded to a UTF‑8 byte array.
    /// </summary>
    /// <returns>Nothing.</returns>
    [Fact]
    public void AsBytes_ExtensionMethod_ConvertsStringToByteArray()
    {
        // Arrange
        var str = "hello";

        // Act
        var bytes = str.AsBytes();

        // Assert
        System.Text.Encoding.UTF8.GetString(bytes).Should().Be(str);
    }
}

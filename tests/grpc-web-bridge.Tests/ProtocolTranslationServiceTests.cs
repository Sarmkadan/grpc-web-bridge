#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using FluentAssertions;
using GrpcWebBridge.Domain;
using GrpcWebBridge.Domain.Exceptions;
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
        _logger.LogInformation("TranslateHttpToGrpc_WithValidInput_ReturnsGrpcRequest started");
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
        _logger.LogInformation("TranslateHttpToGrpc_WithValidInput_ReturnsGrpcRequest completed");
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

    /// <summary>
    /// Verifies that translating a valid gRPC response to HTTP format preserves the payload
    /// when the target format matches the response format.
    /// </summary>
    [Fact]
    public void TranslateGrpcToHttp_WithMatchingFormats_PreservesPayload()
    {
        // Arrange
        var request = new GrpcRequest("TestService", "TestMethod", "payload".AsBytes());
        var response = new GrpcResponse(request.Id, "payload".AsBytes())
        {
            PayloadFormat = SerializationFormat.Protobuf
        };

        // Act
        var httpPayload = _service.TranslateGrpcToHttp(response, SerializationFormat.Protobuf);

        // Assert
        httpPayload.Should().BeEquivalentTo("payload".AsBytes());
    }

    /// <summary>
    /// Ensures that translating a gRPC response to JSON converts the payload from Protobuf
    /// to JSON format.
    /// </summary>
    [Fact]
    public void TranslateGrpcToHttp_ConvertsProtobufToJson()
    {
        // Arrange
        var request = new GrpcRequest("TestService", "TestMethod", "payload".AsBytes());
        var response = new GrpcResponse(request.Id, "payload".AsBytes())
        {
            PayloadFormat = SerializationFormat.Protobuf
        };

        // Act
        var jsonPayload = _service.TranslateGrpcToHttp(response, SerializationFormat.Json);

        // Assert
        jsonPayload.Should().NotBeNull();
        var json = System.Text.Encoding.UTF8.GetString(jsonPayload);
        json.Should().Contain("data");
    }

    /// <summary>
    /// Confirms that translating a gRPC response to Protobuf converts the payload from JSON
    /// to Protobuf format.
    /// </summary>
    [Fact]
    public void TranslateGrpcToHttp_ConvertsJsonToProtobuf()
    {
        // Arrange
        var jsonPayload = "{\"data\":\"cGF5bG9hZA==\"}".AsBytes();
        var request = new GrpcRequest("TestService", "TestMethod", jsonPayload);
        var response = new GrpcResponse(request.Id, jsonPayload)
        {
            PayloadFormat = SerializationFormat.Json
        };

        // Act
        var protobufPayload = _service.TranslateGrpcToHttp(response, SerializationFormat.Protobuf);

        // Assert
        protobufPayload.Should().NotBeNull();
        var payload = System.Text.Encoding.UTF8.GetString(protobufPayload);
        payload.Should().Be("payload");
    }

    /// <summary>
    /// Validates that TranslateHttpToGrpc throws a ProtocolException when the service name is empty.
    /// </summary>
    [Fact]
    public void TranslateHttpToGrpc_WithEmptyServiceName_ThrowsProtocolException()
    {
        // Arrange
        var payload = "test".AsBytes();

        // Act
        Action act = () => _service.TranslateHttpToGrpc(string.Empty, "TestMethod", payload, SerializationFormat.Json);

        // Assert
        act.Should().Throw<ProtocolException>()
            .And.Message.Should().Contain("Service name cannot be empty");
    }

    /// <summary>
    /// Validates that TranslateHttpToGrpc throws a ProtocolException when the method name is empty.
    /// </summary>
    [Fact]
    public void TranslateHttpToGrpc_WithEmptyMethodName_ThrowsProtocolException()
    {
        // Arrange
        var payload = "test".AsBytes();

        // Act
        Action act = () => _service.TranslateHttpToGrpc("TestService", string.Empty, payload, SerializationFormat.Json);

        // Assert
        act.Should().Throw<ProtocolException>()
            .And.Message.Should().Contain("Method name cannot be empty");
    }

    /// <summary>
    /// Verifies that ValidateRequest throws an exception when the payload exceeds the maximum size.
    /// </summary>
    [Fact]
    public void ValidateRequest_WithPayloadExceedingMaximumSize_ThrowsException()
    {
        // Arrange
        var largePayload = new byte[Constants.Grpc.MaxMessageSize + 1];
        var request = new GrpcRequest("TestService", "TestMethod", largePayload);

        // Act
        Action act = () => _service.ValidateRequest(request);

        // Assert
        act.Should().Throw<Exception>()
            .And.Message.Should().Contain("exceeds maximum size");
    }

    /// <summary>
    /// Ensures that ConvertProtobufToJson converts non-empty Protobuf data to a JSON object
    /// containing a base64-encoded data field.
    /// </summary>
    [Fact]
    public void ConvertProtobufToJson_WithNonEmptyData_ReturnsJsonWithDataField()
    {
        // Arrange
        var protobuf = "test data".AsBytes();

        // Act
        var json = _service.ConvertProtobufToJson(protobuf);

        // Assert
        var jsonString = System.Text.Encoding.UTF8.GetString(json);
        jsonString.Should().Contain("data");
        jsonString.Should().Contain("dGVzdCBkYXRh"); // base64 for "test data"
    }

    /// <summary>
    /// Validates that ConvertJsonToProtobuf throws a ProtocolException when the JSON is malformed.
    /// </summary>
    [Fact]
    public void ConvertJsonToProtobuf_WithMalformedJson_ThrowsProtocolException()
    {
        // Arrange
        var malformedJson = "{ invalid json".AsBytes();

        // Act
        Action act = () => _service.ConvertJsonToProtobuf(malformedJson);

        // Assert
        act.Should().Throw<ProtocolException>();
    }

    /// <summary>
    /// Validates that TranslateAndInvokeAsync throws an ArgumentNullException when the request is null.
    /// </summary>
    [Fact]
    public async Task TranslateAndInvokeAsync_WithNullRequest_ThrowsArgumentNullException()
    {
        _logger.LogInformation("TranslateAndInvokeAsync_WithNullRequest_ThrowsArgumentNullException started");
        // Arrange
        GrpcRequest? request = null;
        AuthenticationContext? authContext = null;

        // Act
        Func<Task> act = async () => await _service.TranslateAndInvokeAsync(request!, authContext);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
        _logger.LogInformation("TranslateAndInvokeAsync_WithNullRequest_ThrowsArgumentNullException completed");
    }
}

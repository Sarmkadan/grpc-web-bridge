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

public class ProtocolTranslationServiceTests
{
    private readonly ILogger<ProtocolTranslationService> _logger;
    private readonly ProtocolTranslationService _service;

    public ProtocolTranslationServiceTests()
    {
        _logger = Substitute.For<ILogger<ProtocolTranslationService>>();
        _service = new ProtocolTranslationService(_logger);
    }

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

    [Fact]
    public void ConvertProtobufToJson_WithEmptyArray_ReturnsEmptyJson()
    {
        // Arrange
        var protobuf = Array.Empty<byte>();

        // Act
        var json = _service.ConvertProtobufToJson(protobuf);

        // Assert
        System.Text.Encoding.UTF8.GetString(json).Should().Be("{}");
    }

    [Fact]
    public void ConvertJsonToProtobuf_WithEmptyArray_ReturnsEmptyArray()
    {
        // Arrange
        var json = Array.Empty<byte>();

        // Act
        var protobuf = _service.ConvertJsonToProtobuf(json);

        // Assert
        protobuf.Should().BeEmpty();
    }

    [Fact]
    public void TranslateMetadata_WithNullMetadata_ReturnsEmptyDictionary()
    {
        // Arrange & Act
        var translated = _service.TranslateMetadata(null!);

        // Assert
        translated.Should().BeEmpty();
        translated.Should().NotBeNull();
    }

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

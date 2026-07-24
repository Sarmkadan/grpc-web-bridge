#nullable enable

using FluentAssertions;
using GrpcWebBridge.BackgroundWorkers;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace GrpcWebBridge.Tests;

/// <summary>
/// Unit tests for <see cref="StreamCleanupWorkerJsonExtensions"/> JSON serialization and deserialization.
/// Tests the three public extension methods: ToJson, FromJson, and TryFromJson.
/// </summary>
public sealed class StreamCleanupWorkerJsonExtensionsTests
{
    private readonly ILogger<StreamCleanupWorker> _logger;

    public StreamCleanupWorkerJsonExtensionsTests()
    {
        _logger = Substitute.For<ILogger<StreamCleanupWorker>>();
    }

    [Fact]
    public void ToJson_WithNullWorker_ThrowsArgumentNullException()
    {
        // Arrange
        StreamCleanupWorker? worker = null;

        // Act
        Action act = () => worker!.ToJson();

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void FromJson_WithNullJson_ThrowsArgumentNullException()
    {
        // Arrange
        string? json = null;

        // Act
        Action act = () => StreamCleanupWorkerJsonExtensions.FromJson(json!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void FromJson_WithEmptyJson_ReturnsNull()
    {
        // Arrange
        var json = string.Empty;

        // Act
        var result = StreamCleanupWorkerJsonExtensions.FromJson(json);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void FromJson_WithWhitespaceJson_ReturnsNull()
    {
        // Arrange
        var json = "   \n\t  ";

        // Act
        var result = StreamCleanupWorkerJsonExtensions.FromJson(json);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void TryFromJson_WithNullJson_ThrowsArgumentNullException()
    {
        // Arrange
        string? json = null;

        // Act
        Action act = () => StreamCleanupWorkerJsonExtensions.TryFromJson(json!, out _);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void TryFromJson_WithEmptyJson_ReturnsFalseAndNull()
    {
        // Arrange
        var json = string.Empty;

        // Act
        var success = StreamCleanupWorkerJsonExtensions.TryFromJson(json, out var result);

        // Assert
        success.Should().BeFalse();
        result.Should().BeNull();
    }

    [Fact]
    public void TryFromJson_WithWhitespaceJson_ReturnsFalseAndNull()
    {
        // Arrange
        var json = "   \n\t  ";

        // Act
        var success = StreamCleanupWorkerJsonExtensions.TryFromJson(json, out var result);

        // Assert
        success.Should().BeFalse();
        result.Should().BeNull();
    }
}

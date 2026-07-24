#nullable enable

using FluentAssertions;
using GrpcWebBridge.BackgroundWorkers;
using GrpcWebBridge.Services;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace GrpcWebBridge.Tests;

public class StreamCleanupWorkerTests
{
    private readonly ILogger<StreamCleanupWorker> _loggerMock;
    private readonly StreamingService _streamingServiceMock;

    public StreamCleanupWorkerTests()
    {
        _loggerMock = Substitute.For<ILogger<StreamCleanupWorker>>();
        var streamingServiceLoggerMock = Substitute.For<ILogger<StreamingService>>();
        _streamingServiceMock = new StreamingService(streamingServiceLoggerMock);
    }

    [Fact]
    public void Constructor_WithNullOptions_UsesDefaultOptions()
    {
        // Arrange
        // Act
        var worker = new StreamCleanupWorker(_loggerMock, _streamingServiceMock, null);

        // Act
        var stats = worker.GetStatistics();

        // Assert
        stats.Should().NotBeNull();
        var expected = new {
            totalCleanupsRun = 0,
            totalStreamsRemoved = 0,
            averageStreamsPerCleanup = 0.0,
            cleanupInterval = 60,
            idleTimeout = 300.0, // 5 minutes * 60
            staleStreamTimeout = 600.0 // 10 minutes * 60
        };
        stats.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void Constructor_WithCustomOptions_UsesCustomOptions()
    {
        // Arrange
        var customOptions = new StreamCleanupOptions
        {
            CleanupIntervalSeconds = 30,
            IdleTimeoutDuration = TimeSpan.FromSeconds(10),
            StaleStreamDuration = TimeSpan.FromSeconds(20),
            GcTriggerThreshold = 5
        };

        // Act
        var worker = new StreamCleanupWorker(_loggerMock, _streamingServiceMock, customOptions);

        // Act
        var stats = worker.GetStatistics();

        // Assert
        stats.Should().NotBeNull();
        var expected = new {
            totalCleanupsRun = 0,
            totalStreamsRemoved = 0,
            averageStreamsPerCleanup = 0.0,
            cleanupInterval = 30,
            idleTimeout = 10.0,
            staleStreamTimeout = 20.0
        };
        stats.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void GetStatistics_ReturnsCorrectInitialValues_WithDefaultOptions()
    {
        // Arrange
        var worker = new StreamCleanupWorker(_loggerMock, _streamingServiceMock, null);

        // Act
        var stats = worker.GetStatistics();

        // Assert
        stats.Should().NotBeNull();
        var expected = new {
            totalCleanupsRun = 0,
            totalStreamsRemoved = 0,
            averageStreamsPerCleanup = 0.0,
            cleanupInterval = 60,
            idleTimeout = 300.0,
            staleStreamTimeout = 600.0
        };
        stats.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void GetStatistics_ReturnsCorrectInitialValues_WithCustomOptions()
    {
        // Arrange
        var customOptions = new StreamCleanupOptions
        {
            CleanupIntervalSeconds = 120,
            IdleTimeoutDuration = TimeSpan.FromMinutes(2), // 120 seconds
            StaleStreamDuration = TimeSpan.FromMinutes(4), // 240 seconds
            GcTriggerThreshold = 20
        };

        var worker = new StreamCleanupWorker(_loggerMock, _streamingServiceMock, customOptions);

        // Act
        var stats = worker.GetStatistics();

        // Assert
        stats.Should().NotBeNull();
        var expected = new {
            totalCleanupsRun = 0,
            totalStreamsRemoved = 0,
            averageStreamsPerCleanup = 0.0,
            cleanupInterval = 120,
            idleTimeout = 120.0, // 2 minutes
            staleStreamTimeout = 240.0 // 4 minutes
        };
        stats.Should().BeEquivalentTo(expected);
    }
}
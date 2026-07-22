#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// Unit tests for CorrelationIdManagerExtensions
// =====================================================================

using System;
using FluentAssertions;
using GrpcWebBridge.Integration;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace GrpcWebBridge.Tests;

public sealed class CorrelationIdManagerExtensionsTests
{
    private readonly ILogger<CorrelationIdManager> _mockLogger;
    private readonly CorrelationIdManager _manager;

    public CorrelationIdManagerExtensionsTests()
    {
        _mockLogger = Substitute.For<ILogger<CorrelationIdManager>>();
        _manager = new CorrelationIdManager(_mockLogger);
    }

    // ─────────────────────────────────────────────────────────────────────
    // HasCorrelationId tests
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public void HasCorrelationId_WhenCorrelationIdIsSet_ReturnsTrue()
    {
        // Arrange
        var correlationId = Guid.NewGuid().ToString();
        _manager.SetCorrelationId(correlationId);

        // Act
        var result = _manager.HasCorrelationId();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void HasCorrelationId_WhenCorrelationIdIsNotSet_ReturnsFalse()
    {
        // Arrange & Act
        var result = _manager.HasCorrelationId();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void HasCorrelationId_WhenCorrelationIdIsEmpty_ReturnsFalse()
    {
        // Arrange
        _manager.SetCorrelationId(string.Empty);

        // Act
        var result = _manager.HasCorrelationId();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void HasCorrelationId_WithNullManager_ThrowsArgumentNullException()
    {
        // Arrange
        CorrelationIdManager? nullManager = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => nullManager!.HasCorrelationId());
    }

    // ─────────────────────────────────────────────────────────────────────
    // GetTraceDuration tests
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public void GetTraceDuration_WhenTraceExistsAndCompleted_ReturnsDuration()
    {
        // Arrange
        var trace = _manager.StartTrace("test-operation");
        var traceId = trace.TraceId;

        // Small delay to ensure duration is measurable
        Thread.Sleep(20);

        _manager.CompleteTrace(traceId);

        // Act
        var duration = _manager.GetTraceDuration(traceId);

        // Assert
        duration.Should().NotBeNull();
        duration.Should().BeGreaterThan(TimeSpan.Zero);
    }

    [Fact]
    public void GetTraceDuration_WhenTraceExistsButIncomplete_ReturnsNull()
    {
        // Arrange
        var trace = _manager.StartTrace("test-operation");
        var traceId = trace.TraceId;

        // Act
        var duration = _manager.GetTraceDuration(traceId);

        // Assert
        duration.Should().BeNull();
    }

    [Fact]
    public void GetTraceDuration_WhenTraceDoesNotExist_ReturnsNull()
    {
        // Arrange & Act
        var duration = _manager.GetTraceDuration("non-existent-trace-id");

        // Assert
        duration.Should().BeNull();
    }

    [Fact]
    public void GetTraceDuration_WithEmptyTraceId_ReturnsNull()
    {
        // Arrange & Act
        var duration = _manager.GetTraceDuration(string.Empty);

        // Assert
        duration.Should().BeNull();
    }

    [Fact]
    public void GetTraceDuration_WithNullManager_ThrowsArgumentNullException()
    {
        // Arrange
        CorrelationIdManager? nullManager = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => nullManager!.GetTraceDuration("trace-id"));
    }

    // ─────────────────────────────────────────────────────────────────────
    // IsTraceSuccessful tests
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public void IsTraceSuccessful_WhenTraceExistsAndSuccessful_ReturnsTrue()
    {
        // Arrange
        var trace = _manager.StartTrace("test-operation");
        var traceId = trace.TraceId;
        _manager.CompleteTrace(traceId, success: true);

        // Act
        var result = _manager.IsTraceSuccessful(traceId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsTraceSuccessful_WhenTraceExistsButFailed_ReturnsFalse()
    {
        // Arrange
        var trace = _manager.StartTrace("test-operation");
        var traceId = trace.TraceId;
        _manager.CompleteTrace(traceId, success: false, errorMessage: "Test error");

        // Act
        var result = _manager.IsTraceSuccessful(traceId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsTraceSuccessful_WhenTraceExistsButIncomplete_ReturnsTrueByDefault()
    {
        // Arrange
        var trace = _manager.StartTrace("test-operation");
        var traceId = trace.TraceId;

        // Act
        var result = _manager.IsTraceSuccessful(traceId);

        // Assert - incomplete traces default to Success=true
        result.Should().BeTrue();
    }

    [Fact]
    public void IsTraceSuccessful_WhenTraceDoesNotExist_ReturnsFalse()
    {
        // Arrange & Act
        var result = _manager.IsTraceSuccessful("non-existent-trace-id");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsTraceSuccessful_WithEmptyTraceId_ReturnsFalse()
    {
        // Arrange & Act
        var result = _manager.IsTraceSuccessful(string.Empty);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsTraceSuccessful_WithNullManager_ThrowsArgumentNullException()
    {
        // Arrange
        CorrelationIdManager? nullManager = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => nullManager!.IsTraceSuccessful("trace-id"));
    }

    // ─────────────────────────────────────────────────────────────────────
    // GetTraceError tests
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public void GetTraceError_WhenTraceExistsAndFailed_ReturnsErrorMessage()
    {
        // Arrange
        var expectedError = "Database connection failed";
        var trace = _manager.StartTrace("test-operation");
        var traceId = trace.TraceId;
        _manager.CompleteTrace(traceId, success: false, errorMessage: expectedError);

        // Act
        var error = _manager.GetTraceError(traceId);

        // Assert
        error.Should().Be(expectedError);
    }

    [Fact]
    public void GetTraceError_WhenTraceExistsAndSuccessful_ReturnsNull()
    {
        // Arrange
        var trace = _manager.StartTrace("test-operation");
        var traceId = trace.TraceId;
        _manager.CompleteTrace(traceId, success: true);

        // Act
        var error = _manager.GetTraceError(traceId);

        // Assert
        error.Should().BeNull();
    }

    [Fact]
    public void GetTraceError_WhenTraceExistsButIncomplete_ReturnsNull()
    {
        // Arrange
        var trace = _manager.StartTrace("test-operation");
        var traceId = trace.TraceId;

        // Act
        var error = _manager.GetTraceError(traceId);

        // Assert
        error.Should().BeNull();
    }

    [Fact]
    public void GetTraceError_WhenTraceDoesNotExist_ReturnsNull()
    {
        // Arrange & Act
        var error = _manager.GetTraceError("non-existent-trace-id");

        // Assert
        error.Should().BeNull();
    }

    [Fact]
    public void GetTraceError_WithEmptyTraceId_ReturnsNull()
    {
        // Arrange & Act
        var error = _manager.GetTraceError(string.Empty);

        // Assert
        error.Should().BeNull();
    }

    [Fact]
    public void GetTraceError_WithNullManager_ThrowsArgumentNullException()
    {
        // Arrange
        CorrelationIdManager? nullManager = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => nullManager!.GetTraceError("trace-id"));
    }

    // ─────────────────────────────────────────────────────────────────────
    // StartTraceWithAutoCorrelation tests
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public void StartTraceWithAutoCorrelation_WhenNoCorrelationIdExists_CreatesNewCorrelationId()
    {
        // Arrange - ensure no correlation ID is set
        _manager.ClearCorrelationId();

        // Act
        var trace = _manager.StartTraceWithAutoCorrelation("test-operation");

        // Assert
        trace.Should().NotBeNull();
        trace.OperationName.Should().Be("test-operation");
        trace.CorrelationId.Should().NotBeNullOrEmpty();
        _manager.HasCorrelationId().Should().BeTrue();
    }

    [Fact]
    public void StartTraceWithAutoCorrelation_WhenCorrelationIdExists_UsesExistingCorrelationId()
    {
        // Arrange
        var expectedCorrelationId = Guid.NewGuid().ToString();
        _manager.SetCorrelationId(expectedCorrelationId);

        // Act
        var trace = _manager.StartTraceWithAutoCorrelation("test-operation");

        // Assert
        trace.Should().NotBeNull();
        trace.CorrelationId.Should().Be(expectedCorrelationId);
    }

    [Fact]
    public void StartTraceWithAutoCorrelation_WithMetadata_AddsMetadataToTrace()
    {
        // Arrange
        var metadata = new Dictionary<string, string>
        {
            { "key1", "value1" },
            { "key2", "value2" }
        };

        // Act
        var trace = _manager.StartTraceWithAutoCorrelation("test-operation", metadata);

        // Assert
        trace.Should().NotBeNull();
        trace.Metadata.Should().ContainKey("key1").WhoseValue.Should().Be("value1");
        trace.Metadata.Should().ContainKey("key2").WhoseValue.Should().Be("value2");
    }

    [Fact]
    public void StartTraceWithAutoCorrelation_WithNullOperationName_ThrowsArgumentNullException()
    {
        // Arrange
        string? nullOperationName = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _manager.StartTraceWithAutoCorrelation(nullOperationName!));
    }

    [Fact]
    public void StartTraceWithAutoCorrelation_WithEmptyOperationName_ThrowsArgumentException()
    {
        // Arrange
        var emptyOperationName = string.Empty;

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _manager.StartTraceWithAutoCorrelation(emptyOperationName));
    }

    [Fact]
    public void StartTraceWithAutoCorrelation_WithNullManager_ThrowsArgumentNullException()
    {
        // Arrange
        CorrelationIdManager? nullManager = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => nullManager!.StartTraceWithAutoCorrelation("operation"));
    }

    // ─────────────────────────────────────────────────────────────────────
    // GetStatisticsFormatted tests
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public void GetStatisticsFormatted_ReturnsNonEmptyString()
    {
        // Arrange
        var trace = _manager.StartTrace("test-operation");
        _manager.CompleteTrace(trace.TraceId);

        // Act
        var stats = _manager.GetStatisticsFormatted();

        // Assert
        stats.Should().NotBeNullOrEmpty();
        stats.Should().Contain("Correlation Statistics:");
        stats.Should().Contain("Total Traces:");
    }

    [Fact]
    public void GetStatisticsFormatted_WithNullManager_ThrowsArgumentNullException()
    {
        // Arrange
        CorrelationIdManager? nullManager = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => nullManager!.GetStatisticsFormatted());
    }

    // ─────────────────────────────────────────────────────────────────────
    // CleanupOldTraces tests
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public void CleanupOldTraces_WithDefaultRetention_RemovesOldCompletedTraces()
    {
        // Arrange
        var trace1 = _manager.StartTrace("operation1");
        var trace2 = _manager.StartTrace("operation2");

        _manager.CompleteTrace(trace1.TraceId);
        _manager.CompleteTrace(trace2.TraceId);

        // Ensure traces are old enough
        Thread.Sleep(20);

        // Act
        var removedCount = _manager.CleanupOldTraces();

        // Assert
        removedCount.Should().BeGreaterThanOrEqualTo(0);
        _manager.HasTraces().Should().BeFalse();
    }

    [Fact]
    public void CleanupOldTraces_WithNullManager_ThrowsArgumentNullException()
    {
        // Arrange
        CorrelationIdManager? nullManager = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => nullManager!.CleanupOldTraces());
    }

    // ─────────────────────────────────────────────────────────────────────
    // GetCurrentTraces tests
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public void GetCurrentTraces_WhenTracesExist_ReturnsTracesForCurrentCorrelation()
    {
        // Arrange
        var correlationId = Guid.NewGuid().ToString();
        _manager.SetCorrelationId(correlationId);

        var trace1 = _manager.StartTrace("operation1");
        var trace2 = _manager.StartTrace("operation2");
        var trace3 = _manager.StartTrace("operation3");

        // Act
        var traces = _manager.GetCurrentTraces();

        // Assert
        traces.Should().HaveCount(3);
        traces.Should().AllSatisfy(t => t.CorrelationId.Should().Be(correlationId));
    }

    [Fact]
    public void GetCurrentTraces_WhenNoCorrelationId_ReturnsEmptyList()
    {
        // Arrange - ensure no correlation ID is set
        _manager.ClearCorrelationId();

        // Act
        var traces = _manager.GetCurrentTraces();

        // Assert
        traces.Should().BeEmpty();
    }

    [Fact]
    public void GetCurrentTraces_WithNullManager_ThrowsArgumentNullException()
    {
        // Arrange
        CorrelationIdManager? nullManager = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => nullManager!.GetCurrentTraces());
    }

    // ─────────────────────────────────────────────────────────────────────
    // HasTraces tests
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public void HasTraces_WhenTracesExist_ReturnsTrue()
    {
        // Arrange
        var correlationId = Guid.NewGuid().ToString();
        _manager.SetCorrelationId(correlationId);

        _manager.StartTrace("operation1");
        _manager.StartTrace("operation2");

        // Act
        var result = _manager.HasTraces();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void HasTraces_WhenNoTracesExist_ReturnsFalse()
    {
        // Arrange
        _manager.SetCorrelationId(Guid.NewGuid().ToString());
        _manager.ClearAllTraces();

        // Act
        var result = _manager.HasTraces();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void HasTraces_WithNullManager_ThrowsArgumentNullException()
    {
        // Arrange
        CorrelationIdManager? nullManager = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => nullManager!.HasTraces());
    }

    // ─────────────────────────────────────────────────────────────────────
    // GetMostRecentTrace tests
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public void GetMostRecentTrace_WhenTracesExist_ReturnsMostRecentTrace()
    {
        // Arrange
        var correlationId = Guid.NewGuid().ToString();
        _manager.SetCorrelationId(correlationId);

        var trace1 = _manager.StartTrace("operation1");
        Thread.Sleep(10);
        var trace2 = _manager.StartTrace("operation2");
        Thread.Sleep(10);
        var trace3 = _manager.StartTrace("operation3");

        // Act
        var mostRecent = _manager.GetMostRecentTrace();

        // Assert
        mostRecent.Should().NotBeNull();
        mostRecent!.OperationName.Should().Be("operation3");
        mostRecent.TraceId.Should().Be(trace3.TraceId);
    }

    [Fact]
    public void GetMostRecentTrace_WhenSingleTraceExists_ReturnsThatTrace()
    {
        // Arrange
        var correlationId = Guid.NewGuid().ToString();
        _manager.SetCorrelationId(correlationId);

        var trace = _manager.StartTrace("operation1");

        // Act
        var mostRecent = _manager.GetMostRecentTrace();

        // Assert
        mostRecent.Should().NotBeNull();
        mostRecent!.TraceId.Should().Be(trace.TraceId);
    }

    [Fact]
    public void GetMostRecentTrace_WhenNoTracesExist_ReturnsNull()
    {
        // Arrange
        _manager.SetCorrelationId(Guid.NewGuid().ToString());
        _manager.ClearAllTraces();

        // Act
        var mostRecent = _manager.GetMostRecentTrace();

        // Assert
        mostRecent.Should().BeNull();
    }

    [Fact]
    public void GetMostRecentTrace_WithNullManager_ThrowsArgumentNullException()
    {
        // Arrange
        CorrelationIdManager? nullManager = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => nullManager!.GetMostRecentTrace());
    }
}
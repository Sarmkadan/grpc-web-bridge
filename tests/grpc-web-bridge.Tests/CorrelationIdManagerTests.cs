#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using FluentAssertions;
using GrpcWebBridge.Integration;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace GrpcWebBridge.Tests;

/// <summary>
/// Tests for the CorrelationIdManager class.
/// Validates correlation ID generation, preservation, format, and async-local flow.
/// </summary>
public sealed class CorrelationIdManagerTests
{
    private readonly ILogger<CorrelationIdManager> _mockLogger;
    private readonly CorrelationIdManager _manager;

    public CorrelationIdManagerTests()
    {
        _mockLogger = Substitute.For<ILogger<CorrelationIdManager>>();
        _manager = new CorrelationIdManager(_mockLogger);
    }

    [Fact]
    /// <summary>
    /// Validates that GetOrCreateCorrelationId generates a valid GUID-based ID.
    /// </summary>
    public void GetOrCreateCorrelationId_WhenNoIdSet_GeneratesValidGuid()
    {
        // Arrange & Act
        var id1 = _manager.GetOrCreateCorrelationId();
        var id2 = _manager.GetOrCreateCorrelationId();

        // Assert
        _mockLogger.Received(1).LogInformation(
            Arg.Is<string>("Generated correlation ID: {CorrelationId}"),
            Arg.Is<string>(id1));
        id1.Should().NotBeNullOrEmpty();
        id2.Should().NotBeNullOrEmpty();
        // Both calls should return the same ID since it's stored in AsyncLocal
        id1.Should().Be(id2);
        Guid.TryParse(id1, out _).Should().BeTrue("Generated ID should be a valid GUID");
        Guid.TryParse(id2, out _).Should().BeTrue("Generated ID should be a valid GUID");
    }

    [Fact]
    /// <summary>
    /// Validates that GetOrCreateCorrelationId returns existing ID if already set.
    /// </summary>
    public void GetOrCreateCorrelationId_WhenIdAlreadySet_ReturnsExistingId()
    {
        // Arrange
        var expectedId = Guid.NewGuid().ToString();
        _manager.SetCorrelationId(expectedId);

        // Act
        var actualId = _manager.GetOrCreateCorrelationId();

        // Assert
        _mockLogger.Received(1).LogInformation(
            Arg.Is<string>("Using existing correlation ID: {CorrelationId}"),
            Arg.Is<string>(expectedId));
        actualId.Should().Be(expectedId);
    }

    [Fact]
    /// <summary>
    /// Validates that SetCorrelationId throws on null or empty ID.
    /// </summary>
    public void SetCorrelationId_WithNullOrEmpty_ThrowsArgumentException()
    {
        // Arrange
        string? nullId = null;
        string emptyId = string.Empty;

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _manager.SetCorrelationId(nullId!));
        Assert.Throws<ArgumentException>(() => _manager.SetCorrelationId(emptyId));

        _mockLogger.Received(2).LogWarning(
            Arg.Is<string>("Attempted to set invalid correlation ID: {CorrelationId}"),
            Arg.Any<object>());
    }

    [Fact]
    /// <summary>
    /// Validates that SetCorrelationId preserves the incoming correlation ID.
    /// </summary>
    public void SetCorrelationId_WithValidId_PreservesIncomingId()
    {
        // Arrange
        var expectedId = "custom-correlation-id-12345";

        // Act
        _manager.SetCorrelationId(expectedId);
        var actualId = _manager.GetCorrelationId();

        // Assert
        _mockLogger.Received(1).LogInformation(
            Arg.Is<string>("Set correlation ID: {CorrelationId}"),
            Arg.Is<string>(expectedId));
        actualId.Should().Be(expectedId);
    }

    [Fact]
    /// <summary>
    /// Validates that GetCorrelationId returns null when no ID is set.
    /// </summary>
    public void GetCorrelationId_WhenNoIdSet_ReturnsNull()
    {
        // Arrange & Act
        var id = _manager.GetCorrelationId();

        // Assert
        _mockLogger.Received(1).LogInformation(
            Arg.Is<string>("No correlation ID found, returning null"));
        id.Should().BeNull();
    }

    [Fact]
    /// <summary>
    /// Validates that GetCorrelationId returns the current correlation ID.
    /// </summary>
    public void GetCorrelationId_WhenIdSet_ReturnsCurrentId()
    {
        // Arrange
        var expectedId = Guid.NewGuid().ToString();
        _manager.SetCorrelationId(expectedId);

        // Act
        var actualId = _manager.GetCorrelationId();

        // Assert
        _mockLogger.Received(1).LogInformation(
            Arg.Is<string>("Retrieved correlation ID: {CorrelationId}"),
            Arg.Is<string>(expectedId));
        actualId.Should().Be(expectedId);
    }

    [Fact]
    /// <summary>
    /// Validates that ClearCorrelationId clears the current correlation ID.
    /// </summary>
    public void ClearCorrelationId_RemovesCurrentId()
    {
        // Arrange
        var expectedId = Guid.NewGuid().ToString();
        _manager.SetCorrelationId(expectedId);
        _manager.GetCorrelationId().Should().Be(expectedId);

        // Act
        _manager.ClearCorrelationId();

        // Assert
        _mockLogger.Received(1).LogInformation(
            Arg.Is<string>("Cleared correlation ID: {CorrelationId}"),
            Arg.Is<string>(expectedId));
        _manager.GetCorrelationId().Should().BeNull();
    }

    [Fact]
    /// <summary>
    /// Validates that async-local flow preserves correlation ID across await boundaries.
    /// </summary>
    public async Task AsyncLocalFlow_PreservesCorrelationIdAcrossAwaits()
    {
        // Arrange
        var expectedId = Guid.NewGuid().ToString();
        _manager.SetCorrelationId(expectedId);

        // Act
        await Task.Delay(10); // Cross await boundary
        var actualId = _manager.GetCorrelationId();

        // Assert
        _mockLogger.Received(1).LogInformation(
            Arg.Is<string>("Accessed correlation ID across await boundary: {CorrelationId}"),
            Arg.Is<string>(expectedId));
        actualId.Should().Be(expectedId);
    }

    [Fact]
    /// <summary>
    /// Validates that StartTrace creates a new trace with correct properties.
    /// </summary>
    public void StartTrace_CreatesNewTraceWithCorrectProperties()
    {
        // Arrange
        var operationName = "test-operation";
        var metadata = new Dictionary<string, string> { { "key1", "value1" } };

        // Act
        var trace = _manager.StartTrace(operationName, metadata: metadata);

        // Assert
        _mockLogger.Received(1).LogInformation(
            Arg.Is<string>("Started trace {TraceId} for operation {OperationName} with correlation ID {CorrelationId}"),
            Arg.Is<string>(trace.TraceId),
            Arg.Is<string>(operationName),
            Arg.Is<string>(trace.CorrelationId));
        trace.Should().NotBeNull();
        trace.TraceId.Should().NotBeNullOrEmpty();
        trace.CorrelationId.Should().NotBeNullOrEmpty();
        trace.OperationName.Should().Be(operationName);
        trace.StartTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        trace.EndTime.Should().BeNull();
        trace.Success.Should().BeTrue();
        trace.Metadata.Should().ContainKey("key1").WhoseValue.Should().Be("value1");
    }

    [Fact]
    /// <summary>
    /// Validates that StartTrace uses existing correlation ID.
    /// </summary>
    public void StartTrace_UsesExistingCorrelationId()
    {
        // Arrange
        var expectedCorrelationId = Guid.NewGuid().ToString();
        _manager.SetCorrelationId(expectedCorrelationId);

        var operationName = "test-operation";

        // Act
        var trace = _manager.StartTrace(operationName);

        // Assert
        _mockLogger.Received(1).LogInformation(
            Arg.Is<string>("Started trace {TraceId} for operation {OperationName} using existing correlation ID {CorrelationId}"),
            Arg.Is<string>(trace.TraceId),
            Arg.Is<string>(operationName),
            Arg.Is<string>(expectedCorrelationId));
        trace.CorrelationId.Should().Be(expectedCorrelationId);
    }

    [Fact]
    /// <summary>
    /// Validates that CompleteTrace updates trace with end time and success status.
    /// </summary>
    public void CompleteTrace_UpdatesTraceWithEndTimeAndSuccess()
    {
        // Arrange
        var trace = _manager.StartTrace("test-operation");
        var traceId = trace.TraceId;
        var startTime = trace.StartTime;

        // Small delay to ensure end time is different
        Thread.Sleep(10);

        // Act
        _manager.CompleteTrace(traceId, success: false, errorMessage: "Test error");

        // Assert
        _mockLogger.Received(1).LogInformation(
            Arg.Is<string>("Completed trace {TraceId} with success: {Success} and error: {ErrorMessage}"),
            Arg.Is<string>(traceId),
            Arg.Is<bool>(false),
            Arg.Is<string>("Test error"));
        var updatedTrace = _manager.GetTrace(traceId);
        updatedTrace.Should().NotBeNull();
        updatedTrace!.EndTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        updatedTrace.Success.Should().BeFalse();
        updatedTrace.ErrorMessage.Should().Be("Test error");
        updatedTrace.GetDuration().Should().BeGreaterThan(TimeSpan.Zero);
    }

    [Fact]
    /// <summary>
    /// Validates that CompleteTrace does nothing when trace ID is null or empty.
    /// </summary>
    public void CompleteTrace_WithNullOrEmptyTraceId_DoesNothing()
    {
        // Arrange
        var trace = _manager.StartTrace("test-operation");

        // Act - should not throw
        _manager.CompleteTrace(null!);
        _manager.CompleteTrace(string.Empty);
        _manager.CompleteTrace("non-existent-id");

        // Assert - trace should remain unchanged
        _mockLogger.Received(3).LogWarning(
            Arg.Is<string>("Attempted to complete trace with invalid ID: {TraceId}"),
            Arg.Any<object>());
        var updatedTrace = _manager.GetTrace(trace.TraceId);
        updatedTrace.Should().NotBeNull();
        updatedTrace!.EndTime.Should().BeNull();
    }

    [Fact]
    /// <summary>
    /// Validates that GetTrace returns null for non-existent trace ID.
    /// </summary>
    public void GetTrace_WithNonExistentId_ReturnsNull()
    {
        // Arrange & Act
        var trace = _manager.GetTrace("non-existent-id");

        // Assert
        _mockLogger.Received(1).LogWarning(
            Arg.Is<string>("Attempted to retrieve non-existent trace with ID: {TraceId}"),
            Arg.Is<string>("non-existent-id"));
        trace.Should().BeNull();
    }

    [Fact]
    /// <summary>
    /// Validates that GetTracesForCorrelation returns all traces for a correlation ID.
    /// </summary>
    public void GetTracesForCorrelation_ReturnsAllTracesForCorrelationId()
    {
        // Arrange
        var correlationId = Guid.NewGuid().ToString();
        _manager.SetCorrelationId(correlationId);

        var trace1 = _manager.StartTrace("operation1");
        var trace2 = _manager.StartTrace("operation2");
        var trace3 = _manager.StartTrace("operation3");

        // Complete some traces
        _manager.CompleteTrace(trace1.TraceId);
        _manager.CompleteTrace(trace3.TraceId);

        // Act
        var traces = _manager.GetTracesForCorrelation(correlationId);

        // Assert
        _mockLogger.Received(1).LogInformation(
            Arg.Is<string>("Retrieved {Count} traces for correlation ID {CorrelationId}"),
            Arg.Is<int>(3),
            Arg.Is<string>(correlationId));
        traces.Should().HaveCount(3);
        traces.Should().AllSatisfy(t => t.CorrelationId.Should().Be(correlationId));
        traces.Should().BeInAscendingOrder(t => t.StartTime);
    }

    [Fact]
    /// <summary>
    /// Validates that GetTracesForCorrelation returns empty list for null or empty correlation ID.
    /// </summary>
    public void GetTracesForCorrelation_WithNullOrEmpty_ReturnsEmptyList()
    {
        // Arrange & Act
        var traces1 = _manager.GetTracesForCorrelation(null!);
        var traces2 = _manager.GetTracesForCorrelation(string.Empty);

        // Assert
        _mockLogger.Received(2).LogWarning(
            Arg.Is<string>("Attempted to get traces for invalid correlation ID: {CorrelationId}"),
            Arg.Any<object>());
        traces1.Should().BeEmpty();
        traces2.Should().BeEmpty();
    }

    [Fact]
    /// <summary>
    /// Validates that AddTraceMetadata adds metadata to existing trace.
    /// </>
    public void AddTraceMetadata_AddsMetadataToExistingTrace()
    {
        // Arrange
        var trace = _manager.StartTrace("test-operation");
        var traceId = trace.TraceId;
        var key = "custom-key";
        var value = "custom-value";

        // Act
        _manager.AddTraceMetadata(traceId, key, value);

        // Assert
        _mockLogger.Received(1).LogInformation(
            Arg.Is<string>("Added metadata {Key}={Value} to trace {TraceId}"),
            Arg.Is<string>(key),
            Arg.Is<string>(value),
            Arg.Is<string>(traceId));
        var updatedTrace = _manager.GetTrace(traceId);
        updatedTrace.Should().NotBeNull();
        updatedTrace!.Metadata.Should().ContainKey(key).WhoseValue.Should().Be(value);
    }

    [Fact]
    /// <summary>
    /// Validates that AddTraceMetadata does nothing for null or empty parameters.
    /// </summary>
    public void AddTraceMetadata_WithNullOrEmptyParameters_DoesNothing()
    {
        // Arrange
        var trace = _manager.StartTrace("test-operation");

        // Act - should not throw
        _manager.AddTraceMetadata(null!, "key", "value");
        _manager.AddTraceMetadata(string.Empty, "key", "value");
        _manager.AddTraceMetadata(trace.TraceId, null!, "value");
        _manager.AddTraceMetadata(trace.TraceId, string.Empty, "value");

        // Assert - trace should remain unchanged
        _mockLogger.Received(4).LogWarning(
            Arg.Is<string>("Attempted to add trace metadata with invalid parameters: TraceId={TraceId}, Key={Key}, Value={Value}"),
            Arg.Any<object>(),
            Arg.Any<object>(),
            Arg.Any<object>());
        var updatedTrace = _manager.GetTrace(trace.TraceId);
        updatedTrace.Should().NotBeNull();
        updatedTrace!.Metadata.Should().BeEmpty();
    }

    [Fact]
    /// <summary>
    /// Validates that GetStatistics returns correct statistics.
    /// </summary>
    public void GetStatistics_ReturnsCorrectStatistics()
    {
        // Arrange
        var correlationId = Guid.NewGuid().ToString();
        _manager.SetCorrelationId(correlationId);

        var trace1 = _manager.StartTrace("operation1");
        var trace2 = _manager.StartTrace("operation2");
        var trace3 = _manager.StartTrace("operation3");

        // Complete with mixed success
        _manager.CompleteTrace(trace1.TraceId, success: true);
        _manager.CompleteTrace(trace2.TraceId, success: false, errorMessage: "Error");
        // trace3 remains incomplete

        // Act
        var stats = _manager.GetStatistics();

        // Assert
        _mockLogger.Received(1).LogInformation(
            Arg.Is<string>("Retrieved trace statistics: Total={Total}, Completed={Completed}, Active={Active}, Successful={Successful}, Failed={Failed}, AvgDurationMs={AverageDurationMs}, UniqueCorrelations={UniqueCorrelations}"),
            Arg.Is<int>(3),
            Arg.Is<int>(2),
            Arg.Is<int>(1),
            Arg.Is<int>(1),
            Arg.Is<int>(1),
            Arg.Is<double>(Arg.Any<double>()),
            Arg.Is<int>(1));
        stats.Should().NotBeNull();

        // Use reflection to access anonymous type properties
        var totalTraces = (int)stats.GetType().GetProperty("totalTraces")!.GetValue(stats)!;
        var completedTraces = (int)stats.GetType().GetProperty("completedTraces")!.GetValue(stats)!;
        var activeTraces = (int)stats.GetType().GetProperty("activeTraces")!.GetValue(stats)!;
        var successfulTraces = (int)stats.GetType().GetProperty("successfulTraces")!.GetValue(stats)!;
        var failedTraces = (int)stats.GetType().GetProperty("failedTraces")!.GetValue(stats)!;
        var averageDurationMs = (double)stats.GetType().GetProperty("averageDurationMs")!.GetValue(stats)!;
        var totalUniqueCorrelations = (int)stats.GetType().GetProperty("totalUniqueCorrelations")!.GetValue(stats)!;

        totalTraces.Should().Be(3);
        completedTraces.Should().Be(2);
        activeTraces.Should().Be(1);
        successfulTraces.Should().Be(1);
        failedTraces.Should().Be(1);
        totalUniqueCorrelations.Should().Be(1);
        averageDurationMs.Should().BeGreaterThan(0);
    }

    [Fact]
    /// <summary>
    /// Validates that CleanupOldTraces removes traces older than specified duration.
    /// </summary>
    public void CleanupOldTraces_RemovesOldTraces()
    {
        // Arrange
        var trace1 = _manager.StartTrace("operation1");
        var trace2 = _manager.StartTrace("operation2");

        _manager.CompleteTrace(trace1.TraceId);
        _manager.CompleteTrace(trace2.TraceId);

        // Ensure traces are old enough (older than 1 second)
        Thread.Sleep(100);

        // Act - cleanup traces older than 1 millisecond (should remove all completed traces)
        var removedCount = _manager.CleanupOldTraces(TimeSpan.FromMilliseconds(1));

        // Assert
        _mockLogger.Received(1).LogInformation(
            Arg.Is<string>("Cleaned up {Count} old traces older than {Duration}"),
            Arg.Is<int>(2),
            Arg.Is<TimeSpan>(TimeSpan.FromMilliseconds(1)));
        removedCount.Should().Be(2);
        _manager.GetTrace(trace1.TraceId).Should().BeNull();
        _manager.GetTrace(trace2.TraceId).Should().BeNull();
    }

    [Fact]
    /// <summary>
    /// Validates that CleanupOldTraces does not remove recently completed traces.
    /// </summary>
    public void CleanupOldTraces_LeavesRecentlyCompletedTraces()
    {
        // Arrange
        var trace1 = _manager.StartTrace("operation1");
        var trace2 = _manager.StartTrace("operation2");

        _manager.CompleteTrace(trace1.TraceId);
        _manager.CompleteTrace(trace2.TraceId);

        // Act - cleanup traces older than 1 hour (should not remove anything)
        var removedCount = _manager.CleanupOldTraces(TimeSpan.FromHours(1));

        // Assert
        _mockLogger.Received(1).LogInformation(
            Arg.Is<string>("Cleaned up {Count} old traces older than {Duration}"),
            Arg.Is<int>(0),
            Arg.Is<TimeSpan>(TimeSpan.FromHours(1)));
        removedCount.Should().Be(0);
        _manager.GetTrace(trace1.TraceId).Should().NotBeNull();
        _manager.GetTrace(trace2.TraceId).Should().NotBeNull();
    }

    [Fact]
    /// <summary>
    /// Validates that ClearAllTraces removes all traces.
    /// </summary>
    public void ClearAllTraces_RemovesAllTraces()
    {
        // Arrange
        var trace1 = _manager.StartTrace("operation1");
        var trace2 = _manager.StartTrace("operation2");
        var trace3 = _manager.StartTrace("operation3");

        _manager.CompleteTrace(trace1.TraceId);
        _manager.CompleteTrace(trace2.TraceId);

        // Act
        _manager.ClearAllTraces();

        // Assert
        _mockLogger.Received(1).LogInformation(
            Arg.Is<string>("Cleared all traces. Removed {Count} traces."),
            Arg.Is<int>(3));
        _manager.GetTrace(trace1.TraceId).Should().BeNull();
        _manager.GetTrace(trace2.TraceId).Should().BeNull();
        _manager.GetTrace(trace3.TraceId).Should().BeNull();
    }

    [Fact]
    /// <summary>
    /// Validates that CorrelationTrace.GetDuration returns correct duration.
    /// </summary>
    public void CorrelationTrace_GetDuration_ReturnsCorrectDuration()
    {
        // Arrange
        var trace = _manager.StartTrace("test-operation");
        var traceId = trace.TraceId;
        Thread.Sleep(50); // Ensure some time passes

        // Act
        _manager.CompleteTrace(traceId);
        var updatedTrace = _manager.GetTrace(traceId);
        var duration = updatedTrace?.GetDuration();

        // Assert
        _mockLogger.Received(1).LogInformation(
            Arg.Is<string>("Calculated duration for trace {TraceId}: {Duration}"),
            Arg.Is<string>(traceId),
            Arg.Is<TimeSpan>(Arg.Any<TimeSpan>()));
        duration.Should().NotBeNull();
        duration.Should().BeGreaterThan(TimeSpan.Zero);
        duration.Should().BeCloseTo(TimeSpan.FromMilliseconds(50), TimeSpan.FromMilliseconds(20));
    }

    [Fact]
    /// <summary>
    /// Validates that CorrelationTrace.GetDuration returns null for incomplete trace.
    /// </summary>
    public void CorrelationTrace_GetDuration_ForIncompleteTrace_ReturnsNull()
    {
        // Arrange
        var trace = _manager.StartTrace("test-operation");
        var duration = trace.GetDuration();

        // Assert
        _mockLogger.Received(1).LogInformation(
            Arg.Is<string>("Attempted to get duration for incomplete trace {TraceId}"),
            Arg.Is<string>(trace.TraceId));
        duration.Should().BeNull();
    }
}
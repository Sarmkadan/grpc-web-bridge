#nullable enable

using FluentAssertions;
using GrpcWebBridge.BackgroundWorkers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.Reflection;
using Xunit;

namespace GrpcWebBridge.Tests;

public sealed class MetricsCollectionWorkerTests
{
    private readonly ILogger<MetricsCollectionWorker> _logger;
    private readonly MetricsCollectionOptions _options;

    public MetricsCollectionWorkerTests()
    {
        _logger = new NullLogger<MetricsCollectionWorker>();
        _options = new MetricsCollectionOptions
        {
            CollectionIntervalSeconds = 30,
            MaxSnapshotsToKeep = 10,
            CpuAlertThresholdPercent = 80,
            MemoryAlertThresholdMb = 1024,
            ErrorRateAlertThresholdPercent = 5
        };
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Act
        Action act = () => new MetricsCollectionWorker(null!, _options);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_NullOptions_CreatesWorkerWithDefaultOptions()
    {
        // Act
        var worker = new MetricsCollectionWorker(_logger, null);

        // Assert
        worker.Should().NotBeNull();
        worker.GetType().GetField("_options", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(worker)
            ?.GetType()
            .Should().Be(typeof(MetricsCollectionOptions));
    }

    [Fact]
    public void Constructor_ValidParameters_CreatesWorkerSuccessfully()
    {
        // Act
        var worker = new MetricsCollectionWorker(_logger, _options);

        // Assert
        worker.Should().NotBeNull();
    }

    [Fact]
    public void GetAggregatedMetrics_NoHistory_ReturnsNoDataMessage()
    {
        // Arrange
        var worker = new MetricsCollectionWorker(_logger, _options);

        // Act
        var result = worker.GetAggregatedMetrics();

        // Assert
        result.Should().NotBeNull();
        var resultType = result.GetType();
        resultType.GetProperty("message")?.GetValue(result)?.ToString()
            .Should().Be("No metrics data available for the specified time period");
    }

    [Fact]
    public void GetAggregatedMetrics_AfterClearHistory_ReturnsNoDataMessage()
    {
        // Arrange
        var worker = new MetricsCollectionWorker(_logger, _options);
        worker.ClearHistory();

        // Act
        var result = worker.GetAggregatedMetrics();

        // Assert
        result.Should().NotBeNull();
        var resultType = result.GetType();
        resultType.GetProperty("message")?.GetValue(result)?.ToString()
            .Should().Be("No metrics data available for the specified time period");
    }

    [Fact]
    public void GetAggregatedMetrics_WithRecentHistory_ReturnsAggregatedData()
    {
        // Arrange
        var worker = new MetricsCollectionWorker(_logger, _options);
        var now = DateTime.UtcNow;

        var snapshot1 = new MetricsSnapshot
        {
            Timestamp = now.AddMinutes(-2),
            CpuUsagePercent = 45.5,
            MemoryUsageMb = 512.0,
            ThreadCount = 10,
            GcCollections = new { Gen0 = 5, Gen1 = 2, Gen2 = 1 },
            RequestMetrics = new MetricsSnapshot.RequestMetricsData
            {
                TotalRequests = 100,
                TotalErrors = 5,
                ErrorRate = 5.0
            }
        };

        var snapshot2 = new MetricsSnapshot
        {
            Timestamp = now.AddMinutes(-1),
            CpuUsagePercent = 55.0,
            MemoryUsageMb = 600.0,
            ThreadCount = 12,
            GcCollections = new { Gen0 = 7, Gen1 = 3, Gen2 = 1 },
            RequestMetrics = new MetricsSnapshot.RequestMetricsData
            {
                TotalRequests = 150,
                TotalErrors = 8,
                ErrorRate = 5.33
            }
        };

        var snapshot3 = new MetricsSnapshot
        {
            Timestamp = now,
            CpuUsagePercent = 60.0,
            MemoryUsageMb = 550.0,
            ThreadCount = 11,
            GcCollections = new { Gen0 = 6, Gen1 = 2, Gen2 = 1 },
            RequestMetrics = new MetricsSnapshot.RequestMetricsData
            {
                TotalRequests = 200,
                TotalErrors = 10,
                ErrorRate = 5.0
            }
        };

        var historyField = typeof(MetricsCollectionWorker).GetField("_snapshotHistory", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var lockField = typeof(MetricsCollectionWorker).GetField("_lockObject", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var queue = (Queue<MetricsSnapshot>)historyField.GetValue(worker)!;
        var lockObj = lockField.GetValue(worker);

        lock (lockObj!)
        {
            queue.Clear();
            queue.Enqueue(snapshot1);
            queue.Enqueue(snapshot2);
            queue.Enqueue(snapshot3);
        }

        // Act
        var result = worker.GetAggregatedMetrics(lastMinutes: 5);

        // Assert
        result.Should().NotBeNull();
        var resultType = result.GetType();

        resultType.GetProperty("period")?.GetValue(result)?.ToString().Should().Be("Last 5 minutes");
        resultType.GetProperty("snapshotCount")?.GetValue(result)?.Should().Be(3);

        var cpuObj = resultType.GetProperty("cpu")?.GetValue(result);
        cpuObj.Should().NotBeNull();
        var cpuType = cpuObj.GetType();
        cpuType.GetProperty("average")?.GetValue(cpuObj)?.Should().Be(53.5);
        cpuType.GetProperty("min")?.GetValue(cpuObj)?.Should().Be(45.5);
        cpuType.GetProperty("max")?.GetValue(cpuObj)?.Should().Be(60.0);

        var memoryObj = resultType.GetProperty("memory")?.GetValue(result);
        memoryObj.Should().NotBeNull();
        var memoryType = memoryObj.GetType();
        memoryType.GetProperty("average")?.GetValue(memoryObj)?.Should().Be(554.0);
        memoryType.GetProperty("min")?.GetValue(memoryObj)?.Should().Be(512.0);
        memoryType.GetProperty("max")?.GetValue(memoryObj)?.Should().Be(600.0);

        var threadsObj = resultType.GetProperty("threads")?.GetValue(result);
        threadsObj.Should().NotBeNull();
        var threadsType = threadsObj.GetType();
        threadsType.GetProperty("average")?.GetValue(threadsObj)?.Should().Be(11);
        threadsType.GetProperty("min")?.GetValue(threadsObj)?.Should().Be(10);
        threadsType.GetProperty("max")?.GetValue(threadsObj)?.Should().Be(12);

        var latestRequestsObj = resultType.GetProperty("latestRequests")?.GetValue(result);
        latestRequestsObj.Should().NotBeNull();
        var latestRequestsType = latestRequestsObj.GetType();
        latestRequestsType.GetProperty("TotalRequests")?.GetValue(latestRequestsObj)?.Should().Be(200L);
        latestRequestsType.GetProperty("TotalErrors")?.GetValue(latestRequestsObj)?.Should().Be(10L);
        latestRequestsType.GetProperty("ErrorRate")?.GetValue(latestRequestsObj)?.Should().Be(5.0);
    }

    [Fact]
    public void GetSnapshotHistory_ReturnsSnapshotsInOrder()
    {
        // Arrange
        var worker = new MetricsCollectionWorker(_logger, _options);
        var now = DateTime.UtcNow;

        var snapshot1 = new MetricsSnapshot { Timestamp = now.AddMinutes(-2), CpuUsagePercent = 10.0 };
        var snapshot2 = new MetricsSnapshot { Timestamp = now.AddMinutes(-1), CpuUsagePercent = 20.0 };
        var snapshot3 = new MetricsSnapshot { Timestamp = now, CpuUsagePercent = 30.0 };

        var historyField = typeof(MetricsCollectionWorker).GetField("_snapshotHistory", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var lockField = typeof(MetricsCollectionWorker).GetField("_lockObject", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var queue = (Queue<MetricsSnapshot>)historyField.GetValue(worker)!;
        var lockObj = lockField.GetValue(worker);

        lock (lockObj!)
        {
            queue.Clear();
            queue.Enqueue(snapshot1);
            queue.Enqueue(snapshot2);
            queue.Enqueue(snapshot3);
        }

        // Act
        var result = worker.GetSnapshotHistory();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(3);
        result[0].Timestamp.Should().Be(snapshot1.Timestamp);
        result[0].CpuUsagePercent.Should().Be(10.0);
        result[1].Timestamp.Should().Be(snapshot2.Timestamp);
        result[1].CpuUsagePercent.Should().Be(20.0);
        result[2].Timestamp.Should().Be(snapshot3.Timestamp);
        result[2].CpuUsagePercent.Should().Be(30.0);
    }

    [Fact]
    public void GetSnapshotHistory_AfterClearHistory_ReturnsEmptyList()
    {
        // Arrange
        var worker = new MetricsCollectionWorker(_logger, _options);
        var now = DateTime.UtcNow;
        var snapshot = new MetricsSnapshot { Timestamp = now, CpuUsagePercent = 50.0 };

        var historyField = typeof(MetricsCollectionWorker).GetField("_snapshotHistory", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var lockField = typeof(MetricsCollectionWorker).GetField("_lockObject", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var queue = (Queue<MetricsSnapshot>)historyField.GetValue(worker)!;
        var lockObj = lockField.GetValue(worker);

        lock (lockObj!)
        {
            queue.Clear();
            queue.Enqueue(snapshot);
        }

        // Act
        worker.ClearHistory();
        var result = worker.GetSnapshotHistory();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void ClearHistory_ClearsTheHistory()
    {
        // Arrange
        var worker = new MetricsCollectionWorker(_logger, _options);
        var now = DateTime.UtcNow;
        var snapshot = new MetricsSnapshot { Timestamp = now, CpuUsagePercent = 50.0 };

        var historyField = typeof(MetricsCollectionWorker).GetField("_snapshotHistory", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var lockField = typeof(MetricsCollectionWorker).GetField("_lockObject", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var queue = (Queue<MetricsSnapshot>)historyField.GetValue(worker)!;
        var lockObj = lockField.GetValue(worker);

        lock (lockObj!)
        {
            queue.Clear();
            queue.Enqueue(snapshot);
        }

        // Act
        worker.ClearHistory();

        // Assert
        lock (lockObj!)
        {
            queue.Should().BeEmpty();
        }
    }

    [Fact]
    public void GetAggregatedMetrics_WithZeroMinutes_ReturnsNoDataWhenNoRecentSnapshots()
    {
        // Arrange
        var worker = new MetricsCollectionWorker(_logger, _options);
        var now = DateTime.UtcNow;
        var snapshot = new MetricsSnapshot
        {
            Timestamp = now.AddMinutes(-1), // 1 minute ago
            CpuUsagePercent = 30.0,
            MemoryUsageMb = 100.0,
            ThreadCount = 5,
            GcCollections = new { Gen0 = 0, Gen1 = 0, Gen2 = 0 },
            RequestMetrics = new MetricsSnapshot.RequestMetricsData
            {
                TotalRequests = 10,
                TotalErrors = 0,
                ErrorRate = 0.0
            }
        };

        var historyField = typeof(MetricsCollectionWorker).GetField("_snapshotHistory", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var lockField = typeof(MetricsCollectionWorker).GetField("_lockObject", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var queue = (Queue<MetricsSnapshot>)historyField.GetValue(worker)!;
        var lockObj = lockField.GetValue(worker);

        lock (lockObj!)
        {
            queue.Clear();
            queue.Enqueue(snapshot);
        }

        // Act
        var result = worker.GetAggregatedMetrics(lastMinutes: 0);

        // Assert
        result.Should().NotBeNull();
        var resultType = result.GetType();
        resultType.GetProperty("message")?.GetValue(result)?.ToString()
            .Should().Be("No metrics data available for the specified time period");
    }

    [Fact]
    public void GetAggregatedMetrics_WithNegativeMinutes_TreatedAsLargePositive()
    {
        // Arrange
        var worker = new MetricsCollectionWorker(_logger, _options);
        var now = DateTime.UtcNow;
        var snapshot = new MetricsSnapshot
        {
            Timestamp = now,
            CpuUsagePercent = 30.0,
            MemoryUsageMb = 100.0,
            ThreadCount = 5,
            GcCollections = new { Gen0 = 0, Gen1 = 0, Gen2 = 0 },
            RequestMetrics = new MetricsSnapshot.RequestMetricsData
            {
                TotalRequests = 10,
                TotalErrors = 0,
                ErrorRate = 0.0
            }
        };

        var historyField = typeof(MetricsCollectionWorker).GetField("_snapshotHistory", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var lockField = typeof(MetricsCollectionWorker).GetField("_lockObject", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var queue = (Queue<MetricsSnapshot>)historyField.GetValue(worker)!;
        var lockObj = lockField.GetValue(worker);

        lock (lockObj!)
        {
            queue.Clear();
            queue.Enqueue(snapshot);
        }

        // Act
        var result = worker.GetAggregatedMetrics(lastMinutes: -10); // negative treated as large positive

        // Assert
        result.Should().NotBeNull();
        var resultType = result.GetType();
        resultType.GetProperty("snapshotCount")?.GetValue(result)?.Should().Be(1);
        var cpuObj = resultType.GetProperty("cpu")?.GetValue(result);
        cpuObj.Should().NotBeNull();
        var cpuType = cpuObj.GetType();
        cpuType.GetProperty("average")?.GetValue(cpuObj)?.Should().Be(30.0);
    }
}
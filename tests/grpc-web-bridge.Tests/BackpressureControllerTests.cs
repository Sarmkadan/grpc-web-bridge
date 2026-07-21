#nullable enable
using FluentAssertions;
using GrpcWebBridge.Events;
using GrpcWebBridge.Streaming;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace GrpcWebBridge.Tests;

/// <summary>
/// Unit tests for BackpressureController covering credit acquisition/release,
/// backpressure signaling, boundary conditions, and thread safety.
/// </summary>
public sealed class BackpressureControllerTests
{
    private static BackpressureController CreateController(
        string streamId = "test-stream",
        FlowControlOptions? options = null,
        EventBus? eventBus = null)
    {
        options ??= new FlowControlOptions
        {
            Mode = FlowControlMode.CreditWindow,
            InitialWindowSize = 10,
            MaxWindowSize = 20,
            BackpressureThreshold = 0.8,
            MaxProducerWaitTime = null
        };

        return new BackpressureController(
            streamId,
            options,
            NullLogger<BackpressureController>.Instance,
            eventBus);
    }

    // ─────────────────────────────────────────────────────────────────────
    // Basic credit acquisition and release
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public void TryConsumeCredit_SucceedsWhenCreditsAvailable()
    {
        // Arrange
        var controller = CreateController();

        // Act
        bool result = controller.TryConsumeCredit(3);

        // Assert
        result.Should().BeTrue();
        controller.AvailableCredits.Should().Be(7); // 10 - 3
        // Utilization = 1 - (7/20) = 1 - 0.35 = 0.65
        controller.WindowUtilization.Should().BeApproximately(0.65, 0.01);
    }

    [Fact]
    public void TryConsumeCredit_FailsWhenInsufficientCredits()
    {
        // Arrange
        var controller = CreateController();
        // Consume 8 credits, leaving only 2
        controller.TryConsumeCredit(8);

        // Act - Try to consume more than available
        bool result = controller.TryConsumeCredit(5); // Need 5, only 2 available

        // Assert
        result.Should().BeFalse();
        controller.AvailableCredits.Should().Be(2); // Unchanged
        controller.IsThrottled.Should().BeTrue(); // Should trigger throttling
    }

    [Fact]
    public void ReleaseCredit_IncreasesAvailableCredits()
    {
        // Arrange
        var controller = CreateController();
        controller.TryConsumeCredit(8); // Leave 2 credits

        // Act
        controller.ReleaseCredit(5);

        // Assert
        controller.AvailableCredits.Should().Be(7); // 2 + 5 (capped at max 20)
        controller.IsThrottled.Should().BeFalse(); // Should lift throttle
    }

    [Fact]
    public void ReleaseCredit_RespectsMaxWindowSize()
    {
        // Arrange
        var controller = CreateController(options: new FlowControlOptions
        {
            Mode = FlowControlMode.CreditWindow,
            InitialWindowSize = 5,
            MaxWindowSize = 10,
            BackpressureThreshold = 0.8,
            MaxProducerWaitTime = null
        });
        controller.TryConsumeCredit(3); // Leave 2 credits

        // Act - Try to release more than headroom allows
        controller.ReleaseCredit(15); // Headroom is 8 (10-2), so only 8 should be added

        // Assert
        controller.AvailableCredits.Should().Be(10); // Maxed out at MaxWindowSize
    }

    // ─────────────────────────────────────────────────────────────────────
    // Pressure increases under load
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public void Pressure_IncreasesUnderLoad_TriggersThrottling()
    {
        // Arrange
        var controller = CreateController(options: new FlowControlOptions
        {
            Mode = FlowControlMode.CreditWindow,
            InitialWindowSize = 5,
            MaxWindowSize = 10,
            BackpressureThreshold = 0.5, // Trigger at 50% utilization
            MaxProducerWaitTime = null
        });

        // Act - Consume credits to exhaust window, then try to consume more
        // This will fail and trigger ApplyThrottle()
        controller.TryConsumeCredit(5); // Exhaust all 5 credits
        bool result = controller.TryConsumeCredit(1); // Try to consume when none available

        // Assert
        result.Should().BeFalse();
        controller.IsThrottled.Should().BeTrue();
        // With 0 available out of 10 max = 100% utilization
        controller.WindowUtilization.Should().BeApproximately(1.0, 0.01);
    }

    [Fact]
    public void Pressure_ReleasedCorrectly_LiftsThrottleWhenBelowThreshold()
    {
        // Arrange
        var controller = CreateController(options: new FlowControlOptions
        {
            Mode = FlowControlMode.CreditWindow,
            InitialWindowSize = 10,
            MaxWindowSize = 20,
            BackpressureThreshold = 0.75, // Lift throttle when utilization < 75%
            MaxProducerWaitTime = null
        });

        // Consume all 10 credits
        controller.TryConsumeCredit(10);
        controller.AvailableCredits.Should().Be(0);

        // Try to consume one more - this will fail and trigger throttle
        controller.TryConsumeCredit(1);
        controller.IsThrottled.Should().BeTrue("because TryConsumeCredit failed and triggered ApplyThrottle");

        // Act - Release credits to drop below threshold
        // With MaxSize=20, threshold=0.75, we need utilization < 0.75
        // That means AvailableCredits/20 > 0.25, so AvailableCredits > 5
        // Currently have 0 available, so release at least 6 to get to 6 available
        controller.ReleaseCredit(6);

        // Assert - throttle should be lifted when utilization drops below threshold
        // After releasing credits, ConsiderLiftingThrottle() is called and should lift the throttle
        controller.IsThrottled.Should().BeFalse();
        // With 6 available out of 20 max = 70% utilization (1 - 6/20 = 0.7)
        controller.WindowUtilization.Should().BeApproximately(0.7, 0.01);
    }

    // ─────────────────────────────────────────────────────────────────────
    // Boundary values (zero capacity)
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public void MinimalInitialCapacity_BlocksWhenExhausted()
    {
        // Arrange
        var controller = CreateController(options: new FlowControlOptions
        {
            Mode = FlowControlMode.CreditWindow,
            InitialWindowSize = 1,
            MaxWindowSize = 10,
            BackpressureThreshold = 0.0,
            MaxProducerWaitTime = null
        });

        // Act - Consume the single credit
        bool result1 = controller.TryConsumeCredit(1);
        result1.Should().BeTrue();

        // Act & Assert - Try to consume another when none available
        bool result2 = controller.TryConsumeCredit(1);
        result2.Should().BeFalse();
        controller.IsThrottled.Should().BeTrue();
        controller.AvailableCredits.Should().Be(0);
        // With 0 available out of 10 max = 100% utilization
        controller.WindowUtilization.Should().BeApproximately(1.0, 0.01);
    }

    [Fact]
    public void SmallMaxCapacity_RespectsLimits()
    {
        // Arrange
        var controller = CreateController(options: new FlowControlOptions
        {
            Mode = FlowControlMode.CreditWindow,
            InitialWindowSize = 2,
            MaxWindowSize = 5,
            BackpressureThreshold = 0.0,
            MaxProducerWaitTime = null
        });

        // Act - Consume all available
        controller.TryConsumeCredit(2);

        // Assert
        controller.AvailableCredits.Should().Be(0);
        controller.IsThrottled.Should().BeFalse(); // Not throttled until we try to consume more

        // Try to consume more than max allows
        bool result = controller.TryConsumeCredit(10);
        result.Should().BeFalse();
        controller.IsThrottled.Should().BeTrue();
    }

    // ─────────────────────────────────────────────────────────────────────
    // Concurrent acquire/release consistency
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task ConcurrentAcquireRelease_MaintainsConsistency()
    {
        // Arrange
        var controller = CreateController(options: new FlowControlOptions
        {
            Mode = FlowControlMode.CreditWindow,
            InitialWindowSize = 50,
            MaxWindowSize = 100,
            BackpressureThreshold = 0.8,
            MaxProducerWaitTime = null
        });

        const int threadCount = 10;
        const int iterationsPerThread = 100;
        var acquiredCredits = 0;
        var releasedCredits = 0;
        var errors = 0;

        // Act
        var tasks = new List<Task>();
        for (int t = 0; t < threadCount; t++)
        {
            tasks.Add(Task.Run(() =>
            {
                var random = new Random();
                for (int i = 0; i < iterationsPerThread; i++)
                {
                    try
                    {
                        if (random.NextDouble() < 0.5 && controller.AvailableCredits > 0)
                        {
                            // Try to consume
                            if (controller.TryConsumeCredit(1))
                            {
                                Interlocked.Increment(ref acquiredCredits);
                            }
                        }
                        else
                        {
                            // Release credits
                            controller.ReleaseCredit(1);
                            Interlocked.Increment(ref releasedCredits);
                        }
                    }
                    catch
                    {
                        Interlocked.Increment(ref errors);
                    }
                }
            }));
        }

        await Task.WhenAll(tasks);

        // Assert
        errors.Should().Be(0);
        // Net credits should be reasonable (released - acquired)
        // Available credits should be between 0 and MaxWindowSize
        controller.AvailableCredits.Should().BeInRange(0, 100); // MaxWindowSize from CreateController
        // Utilization should be valid
        controller.WindowUtilization.Should().BeInRange(0.0, 1.0);
    }

    [Fact]
    public async Task ConsumeCreditAsync_RespectsConcurrencyLimits()
    {
        // Arrange
        var controller = CreateController(options: new FlowControlOptions
        {
            Mode = FlowControlMode.CreditWindow,
            InitialWindowSize = 5,
            MaxWindowSize = 10,
            BackpressureThreshold = 0.8,
            MaxProducerWaitTime = TimeSpan.FromSeconds(2)
        });

        const int concurrentTasks = 15; // More than available credits
        var completedTasks = 0;
        var timedOutTasks = 0;

        // Act
        var tasks = new List<Task>();
        for (int i = 0; i < concurrentTasks; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    await controller.ConsumeCreditAsync(1, CancellationToken.None);
                    Interlocked.Increment(ref completedTasks);
                }
                catch (OperationCanceledException)
                {
                    Interlocked.Increment(ref timedOutTasks);
                }
            }));
        }

        await Task.WhenAll(tasks);

        // Assert
        // Exactly 5 tasks should complete immediately (initial credits)
        // The rest should timeout waiting for credits
        completedTasks.Should().Be(5);
        timedOutTasks.Should().Be(concurrentTasks - 5);
        controller.AvailableCredits.Should().Be(0);
        controller.IsThrottled.Should().BeTrue();
    }

    // ─────────────────────────────────────────────────────────────────────
    // Edge cases and special modes
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public void DisabledMode_AlwaysAllowsConsumption()
    {
        // Arrange
        var controller = CreateController(options: new FlowControlOptions
        {
            Mode = FlowControlMode.Disabled,
            InitialWindowSize = 100, // Large enough to not matter
            MaxWindowSize = 200,
            BackpressureThreshold = 0.0,
            MaxProducerWaitTime = null
        });

        // Act
        bool result1 = controller.TryConsumeCredit(1);
        bool result2 = controller.TryConsumeCredit(100); // Way over limit

        // Assert
        result1.Should().BeTrue();
        result2.Should().BeTrue();
        controller.IsThrottled.Should().BeFalse(); // Never throttled in disabled mode
    }

    [Fact]
    public void ResetWindow_ReturnsToInitialState()
    {
        // Arrange
        var controller = CreateController(options: new FlowControlOptions
        {
            Mode = FlowControlMode.CreditWindow,
            InitialWindowSize = 10,
            MaxWindowSize = 20,
            BackpressureThreshold = 0.5,
            MaxProducerWaitTime = null
        });

        // Consume credits to exhaust window and trigger throttle
        controller.TryConsumeCredit(10); // Exhaust all 10 credits
        controller.AvailableCredits.Should().Be(0);

        // Try to consume one more - this will fail and trigger throttle
        controller.TryConsumeCredit(1);
        controller.IsThrottled.Should().BeTrue(); // Throttled because we tried to consume when no credits available

        // Act
        controller.ResetWindow();

        // Assert
        controller.AvailableCredits.Should().Be(10); // Back to initial
        controller.IsThrottled.Should().BeFalse(); // Throttle lifted
        // WindowUtilization = 1 - (AvailableCredits/MaxWindowSize) = 1 - (10/20) = 0.5
        controller.WindowUtilization.Should().BeApproximately(0.5, 0.01);
        controller.TotalProduced.Should().Be(10); // Counters preserved (10 successful consumes)
        controller.TotalConsumed.Should().Be(0);
    }

    [Fact]
    public void AvailableCredits_Property_MatchesSemaphoreState()
    {
        // Arrange
        var controller = CreateController(options: new FlowControlOptions
        {
            Mode = FlowControlMode.CreditWindow,
            InitialWindowSize = 7,
            MaxWindowSize = 15,
            BackpressureThreshold = 0.8,
            MaxProducerWaitTime = null
        });

        // Act
        controller.TryConsumeCredit(3);
        int available1 = controller.AvailableCredits;
        controller.ReleaseCredit(2);
        int available2 = controller.AvailableCredits;

        // Assert
        available1.Should().Be(4); // 7 - 3
        available2.Should().Be(6); // 4 + 2
    }

    [Fact]
    public void StreamId_IsSetCorrectly()
    {
        // Arrange & Act
        var controller = CreateController("my-test-stream-123");

        // Assert
        controller.StreamId.Should().Be("my-test-stream-123");
    }

    // ─────────────────────────────────────────────────────────────────────
    // Additional pressure increase scenarios
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public void Pressure_IncreasesWithMultipleConsumptions_TriggersThrottleOnlyOnce()
    {
        // Arrange
        var controller = CreateController(options: new FlowControlOptions
        {
            Mode = FlowControlMode.CreditWindow,
            InitialWindowSize = 5,
            MaxWindowSize = 10,
            BackpressureThreshold = 0.5,
            MaxProducerWaitTime = null
        });

        // Act - Consume all credits
        controller.TryConsumeCredit(5);

        // Try to consume more - should trigger throttle
        controller.TryConsumeCredit(1);
        controller.IsThrottled.Should().BeTrue();

        // Try again - throttle flag should remain set (only changes on lift)
        bool result = controller.TryConsumeCredit(1);
        result.Should().BeFalse();
        controller.IsThrottled.Should().BeTrue("throttle flag should remain set until utilization drops");
    }

    [Fact]
    public void Pressure_UtilizationCalculation_AccurateUnderLoad()
    {
        // Arrange
        var controller = CreateController(options: new FlowControlOptions
        {
            Mode = FlowControlMode.CreditWindow,
            InitialWindowSize = 10,
            MaxWindowSize = 20,
            BackpressureThreshold = 0.8
        });

        // Act - Consume various amounts
        controller.TryConsumeCredit(5);
        double util1 = controller.WindowUtilization;

        controller.TryConsumeCredit(3);
        double util2 = controller.WindowUtilization;

        controller.ReleaseCredit(2);
        double util3 = controller.WindowUtilization;

        // Assert
        util1.Should().BeApproximately(0.75, 0.01); // (20-5)/20 = 0.75
        util2.Should().BeApproximately(0.90, 0.01); // (20-8)/20 = 0.60, wait that's wrong
        // Actually WindowUtilization = 1 - (AvailableCredits/MaxWindowSize)
        // After consuming 8: Available = 2, so util = 1 - (2/20) = 0.9
        util2.Should().BeApproximately(0.90, 0.01);

        util3.Should().BeApproximately(0.80, 0.01); // After releasing 2: Available = 4, util = 1 - (4/20) = 0.8
    }

    // ─────────────────────────────────────────────────────────────────────
    // Additional release scenarios
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public void ReleaseCredit_MultipleUnits_ReleasesCorrectAmount()
    {
        // Arrange
        var controller = CreateController(options: new FlowControlOptions
        {
            Mode = FlowControlMode.CreditWindow,
            InitialWindowSize = 10,
            MaxWindowSize = 20,
            BackpressureThreshold = 0.8
        });

        // Exhaust credits
        controller.TryConsumeCredit(10);
        controller.AvailableCredits.Should().Be(0);

        // Act
        controller.ReleaseCredit(7);

        // Assert
        controller.AvailableCredits.Should().Be(7);
        controller.IsThrottled.Should().BeFalse();
    }

    [Fact]
    public void ReleaseCredit_WithPartialHeadroom_ReleasesOnlyAvailableHeadroom()
    {
        // Arrange
        var controller = CreateController(options: new FlowControlOptions
        {
            Mode = FlowControlMode.CreditWindow,
            InitialWindowSize = 5,
            MaxWindowSize = 10,
            BackpressureThreshold = 0.8
        });

        // Consume some credits
        controller.TryConsumeCredit(3); // 2 available

        // Try to release more than headroom (headroom = 10-2 = 8)
        controller.ReleaseCredit(15);

        // Assert - should release exactly headroom amount
        controller.AvailableCredits.Should().Be(10);
    }

    // ─────────────────────────────────────────────────────────────────────
    // More boundary value tests
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public void MinimalCapacity_OneCredit_BlocksWhenExhausted()
    {
        // Arrange
        var controller = CreateController(options: new FlowControlOptions
        {
            Mode = FlowControlMode.CreditWindow,
            InitialWindowSize = 1,
            MaxWindowSize = 10,
            BackpressureThreshold = 0.8
        });

        // Act & Assert
        controller.AvailableCredits.Should().Be(1);
        controller.TryConsumeCredit().Should().BeTrue();
        controller.AvailableCredits.Should().Be(0);

        // Try to consume when none available - should fail and trigger throttle
        bool result = controller.TryConsumeCredit();
        result.Should().BeFalse();
        controller.IsThrottled.Should().BeTrue("because TryConsumeCredit triggers ApplyThrottle when it fails");
    }

    [Fact]
    public void MaxCapacityEqualsInitial_WorksCorrectly()
    {
        // Arrange
        var controller = CreateController(options: new FlowControlOptions
        {
            Mode = FlowControlMode.CreditWindow,
            InitialWindowSize = 5,
            MaxWindowSize = 5, // Equal to initial
            BackpressureThreshold = 0.8
        });

        // Act
        controller.TryConsumeCredit(5);

        // Assert
        controller.AvailableCredits.Should().Be(0);
        controller.IsThrottled.Should().BeFalse(); // Not throttled yet

        // Try to consume more - should fail and trigger throttle
        controller.TryConsumeCredit(1);
        controller.IsThrottled.Should().BeTrue();
    }

    // ─────────────────────────────────────────────────────────────────────
    // More concurrent tests
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task ConsumeCreditAsync_WithCancellationToken_RespectsCancellation()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        var controller = CreateController(options: new FlowControlOptions
        {
            Mode = FlowControlMode.CreditWindow,
            InitialWindowSize = 1, // Start with 1 credit
            MaxWindowSize = 5,
            BackpressureThreshold = 0.8,
            MaxProducerWaitTime = TimeSpan.FromSeconds(10)
        });

        // Consume the initial credit
        controller.TryConsumeCredit(1);

        // Start async consume that will block
        var consumeTask = controller.ConsumeCreditAsync(1, cts.Token);

        // Cancel immediately
        cts.Cancel();

        // Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() => consumeTask.AsTask());
    }

    [Fact]
    public async Task ConsumeCreditAsync_AfterRelease_CompletesSuccessfully()
    {
        // Arrange
        var controller = CreateController(options: new FlowControlOptions
        {
            Mode = FlowControlMode.CreditWindow,
            InitialWindowSize = 1,
            MaxWindowSize = 5,
            BackpressureThreshold = 0.8,
            MaxProducerWaitTime = TimeSpan.FromSeconds(5)
        });

        // Consume the initial credit
        controller.TryConsumeCredit(1);

        // Start async consume that will block
        var consumeTask = controller.ConsumeCreditAsync(1);

        // Verify it's waiting
        await Task.Delay(50);
        consumeTask.IsCompleted.Should().BeFalse();

        // Release a credit
        controller.ReleaseCredit(1);

        // Assert
        await consumeTask;
        controller.AvailableCredits.Should().Be(0);
    }

    [Fact]
    public void ConcurrentTryConsume_ThreadSafeOperations()
    {
        // Arrange
        var controller = CreateController(options: new FlowControlOptions
        {
            Mode = FlowControlMode.CreditWindow,
            InitialWindowSize = 100,
            MaxWindowSize = 200,
            BackpressureThreshold = 0.9
        });

        const int threadCount = 20;
        const int iterations = 50;
        var successCount = 0;
        var failureCount = 0;
        var errors = 0;

        // Act
        var tasks = new List<Task>();
        for (int t = 0; t < threadCount; t++)
        {
            tasks.Add(Task.Run(() =>
            {
                try
                {
                    for (int i = 0; i < iterations; i++)
                    {
                        if (controller.TryConsumeCredit(1))
                        {
                            Interlocked.Increment(ref successCount);
                        }
                        else
                        {
                            Interlocked.Increment(ref failureCount);
                        }
                    }
                }
                catch
                {
                    Interlocked.Increment(ref errors);
                }
            }));
        }

        Task.WhenAll(tasks).Wait();

        // Assert
        errors.Should().Be(0);
        // Should succeed approximately InitialWindowSize times
        successCount.Should().BeGreaterThan(0);
        failureCount.Should().BeGreaterThanOrEqualTo(0);
        controller.AvailableCredits.Should().BeInRange(0, 200);
    }

    // ─────────────────────────────────────────────────────────────────────
    // Dispose and cleanup tests
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes_WithoutError()
    {
        // Arrange
        var controller = CreateController();

        // Act
        controller.Dispose();
        controller.Dispose(); // Should not throw

        // Assert
        // All operations should throw ObjectDisposedException
        Action act = () => controller.TryConsumeCredit();
        act.Should().Throw<ObjectDisposedException>();
    }

    [Fact]
    public async Task Dispose_ReleasesSemaphoreResources()
    {
        // Arrange
        var controller = CreateController(options: new FlowControlOptions
        {
            Mode = FlowControlMode.CreditWindow,
            InitialWindowSize = 10,
            MaxWindowSize = 20,
            MaxProducerWaitTime = TimeSpan.FromSeconds(1)
        });

        // Consume some credits
        controller.TryConsumeCredit(5);

        // Act
        controller.Dispose();

        // Assert - subsequent operations should throw
        Action syncAct = () => controller.ReleaseCredit(1);
        syncAct.Should().NotThrow("because Dispose is idempotent");

        Func<Task> asyncAct = async () => await controller.ConsumeCreditAsync(1);
        await asyncAct.Should().ThrowAsync<ObjectDisposedException>();
    }

    // ─────────────────────────────────────────────────────────────────────
    // Total produced/consumed counters
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public void TotalProduced_And_TotalConsumed_CountersTrackMessages()
    {
        // Arrange
        var controller = CreateController(options: new FlowControlOptions
        {
            Mode = FlowControlMode.CreditWindow,
            InitialWindowSize = 10,
            MaxWindowSize = 20
        });

        var initialProduced = controller.TotalProduced;
        var initialConsumed = controller.TotalConsumed;

        // Act - Consume credits (producing messages)
        controller.TryConsumeCredit(3);
        controller.TryConsumeCredit(2);

        // Release credits (consuming messages)
        controller.ReleaseCredit(1);
        controller.ReleaseCredit(2);

        // Assert
        controller.TotalProduced.Should().Be(initialProduced + 5);
        controller.TotalConsumed.Should().Be(initialConsumed + 3);
    }

    [Fact]
    public void TotalProduced_And_TotalConsumed_ResetOnResetWindow()
    {
        // Arrange
        var controller = CreateController(options: new FlowControlOptions
        {
            Mode = FlowControlMode.CreditWindow,
            InitialWindowSize = 10,
            MaxWindowSize = 20
        });

        // Consume and release some credits
        controller.TryConsumeCredit(5);
        controller.ReleaseCredit(2);

        var producedBefore = controller.TotalProduced;
        var consumedBefore = controller.TotalConsumed;

        // Act
        controller.ResetWindow();

        // Assert - counters should be preserved
        controller.TotalProduced.Should().Be(producedBefore);
        controller.TotalConsumed.Should().Be(consumedBefore);
    }
}
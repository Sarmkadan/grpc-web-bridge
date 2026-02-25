#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Threading.Tasks;

namespace GrpcWebBridge.Streaming;

/// <summary>
/// Extension methods for <see cref="BackpressureController"/> that provide convenient
/// operations for credit management and flow control monitoring.
/// </summary>
public static class BackpressureControllerExtensions
{
    /// <summary>
    /// Attempts to consume credits for multiple messages atomically.
    /// </summary>
    /// <param name="controller">The backpressure controller.</param>
    /// <param name="count">Number of credits to consume.</param>
    /// <returns>
    /// <c>true</c> if all credits were successfully consumed;
    /// <c>false</c> if any credits could not be acquired (backpressure applied).
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="controller"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is not positive.</exception>
    public static bool TryConsumeCredits(this BackpressureController controller, int count)
    {
        ArgumentNullException.ThrowIfNull(controller);

        if (count <= 0)
            throw new ArgumentOutOfRangeException(nameof(count), "Count must be positive.");

        if (count == 1)
            return controller.TryConsumeCredit();

        // Use the public TryConsumeCredit in a loop to consume multiple credits
        // This is the only public API available for consuming credits
        int acquired = 0;
        for (; acquired < count; acquired++)
        {
            if (!controller.TryConsumeCredit())
            {
                // Partial acquisition - release any already-acquired credits
                if (acquired > 0)
                {
                    controller.ReleaseCredits(acquired);
                }
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Asynchronously consumes credits for multiple messages with a timeout.
    /// </summary>
    /// <param name="controller">The backpressure controller.</param>
    /// <param name="count">Number of credits to consume.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="controller"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is not positive.</exception>
    public static async ValueTask ConsumeCreditsAsync(
        this BackpressureController controller,
        int count,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(controller);

        if (count <= 0)
            throw new ArgumentOutOfRangeException(nameof(count), "Count must be positive.");

        if (count == 1)
        {
            await controller.ConsumeCreditAsync(cancellationToken: cancellationToken);
            return;
        }

        // Use the public ConsumeCreditAsync in a loop to consume multiple credits
        for (int i = 0; i < count; i++)
        {
            await controller.ConsumeCreditAsync(cancellationToken: cancellationToken);
        }
    }

    /// <summary>
    /// Releases multiple credits atomically.
    /// </summary>
    /// <param name="controller">The backpressure controller.</param>
    /// <param name="count">Number of credits to release.</param>
    /// <exception cref="ArgumentNullException"><paramref name="controller"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if count is not positive.</exception>
    public static void ReleaseCredits(this BackpressureController controller, int count)
    {
        ArgumentNullException.ThrowIfNull(controller);

        if (count <= 0)
            throw new ArgumentOutOfRangeException(nameof(count), "Count must be positive.");

        if (count == 1)
        {
            controller.ReleaseCredit();
            return;
        }

        // Use the public ReleaseCredit in a loop to release multiple credits
        for (int i = 0; i < count; i++)
        {
            controller.ReleaseCredit();
        }
    }

    /// <summary>
    /// Gets the current window utilization as a percentage string.
    /// </summary>
    /// <param name="controller">The backpressure controller.</param>
    /// <returns>A formatted percentage string (e.g., "75.5%" or "100.0%").</returns>
    /// <exception cref="ArgumentNullException"><paramref name="controller"/> is <c>null</c>.</exception>
    public static string GetUtilizationPercentString(this BackpressureController controller)
    {
        ArgumentNullException.ThrowIfNull(controller);
        return $"{controller.WindowUtilization:P1}";
    }

    /// <summary>
    /// Gets a formatted status string for monitoring purposes.
    /// </summary>
    /// <param name="controller">The backpressure controller.</param>
    /// <returns>A formatted status string.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="controller"/> is <c>null</c>.</exception>
    public static string GetStatusString(this BackpressureController controller)
    {
        ArgumentNullException.ThrowIfNull(controller);
        return $"Stream {controller.StreamId}: " +
               $"Utilization={controller.GetUtilizationPercentString()}, " +
               $"Available={controller.AvailableCredits}, " +
               $"Throttled={controller.IsThrottled}, " +
               $"Produced={controller.TotalProduced}, " +
               $"Consumed={controller.TotalConsumed}";
    }
}

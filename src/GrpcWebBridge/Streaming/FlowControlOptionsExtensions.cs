using System;
using System.Threading;
using System.Threading.Channels;

namespace GrpcWebBridge.Streaming
{
    /// <summary>
    /// Provides extension methods for <see cref="FlowControlOptions"/>.
    /// </summary>
    public static class FlowControlOptionsExtensions
    {
        /// <summary>
        /// Validates the specified <see cref="FlowControlOptions"/> instance.
        /// </summary>
        /// <param name="options">The <see cref="FlowControlOptions"/> instance to validate.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when any of the following conditions are met:
        /// <list type="bullet">
        /// <item><description><see cref="FlowControlOptions.InitialWindowSize"/> is less than or equal to zero.</description></item>
        /// <item><description><see cref="FlowControlOptions.MaxWindowSize"/> is less than <see cref="FlowControlOptions.InitialWindowSize"/>.</description></item>
        /// <item><description><see cref="FlowControlOptions.InboundChannelCapacity"/> is less than or equal to zero.</description></item>
        /// <item><description><see cref="FlowControlOptions.OutboundChannelCapacity"/> is less than or equal to zero.</description></item>
        /// <item><description><see cref="FlowControlOptions.BackpressureThreshold"/> is less than 0.0 or greater than 1.0.</description></item>
        /// <item><description><see cref="FlowControlOptions.CreditReplenishmentBatch"/> is less than or equal to zero.</description></item>
        /// <item><description><see cref="FlowControlOptions.MaxProducerWaitTime"/> has a value that is less than zero.</description></item>
        /// <item><description><see cref="FlowControlOptions.AdaptiveAdjustmentInterval"/> is less than zero.</description></item>
        /// </list>
        /// </exception>
        public static void Validate(this FlowControlOptions options)
        {
            if (options.InitialWindowSize <= 0) throw new ArgumentOutOfRangeException(nameof(options.InitialWindowSize), "Must be positive.");
            if (options.MaxWindowSize < options.InitialWindowSize) throw new ArgumentOutOfRangeException(nameof(options.MaxWindowSize), $"Must be >= {nameof(options.InitialWindowSize)} ({options.InitialWindowSize}).");
            if (options.InboundChannelCapacity <= 0) throw new ArgumentOutOfRangeException(nameof(options.InboundChannelCapacity), "Must be positive.");
            if (options.OutboundChannelCapacity <= 0) throw new ArgumentOutOfRangeException(nameof(options.OutboundChannelCapacity), "Must be positive.");
            if (options.BackpressureThreshold < 0.0 || options.BackpressureThreshold > 1.0) throw new ArgumentOutOfRangeException(nameof(options.BackpressureThreshold), "Must be a value between 0.0 and 1.0 inclusive.");
            if (options.CreditReplenishmentBatch <= 0) throw new ArgumentOutOfRangeException(nameof(options.CreditReplenishmentBatch), "Must be positive.");
            if (options.MaxProducerWaitTime.HasValue && options.MaxProducerWaitTime.Value < TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(options.MaxProducerWaitTime), "Must be a positive duration, or null for indefinite wait.");
            if (options.AdaptiveAdjustmentInterval < TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(options.AdaptiveAdjustmentInterval), "Must be positive.");
        }

        /// <summary>
        /// Creates a copy of the specified <see cref="FlowControlOptions"/> instance.
        /// </summary>
        /// <param name="options">The <see cref="FlowControlOptions"/> instance to copy.</param>
        /// <returns>A new <see cref="FlowControlOptions"/> instance with the same property values as the specified instance.</returns>
        public static FlowControlOptions Clone(this FlowControlOptions options)
        {
            return new FlowControlOptions
            {
                InitialWindowSize = options.InitialWindowSize,
                MaxWindowSize = options.MaxWindowSize,
                InboundChannelCapacity = options.InboundChannelCapacity,
                OutboundChannelCapacity = options.OutboundChannelCapacity,
                CreditReplenishmentBatch = options.CreditReplenishmentBatch,
                BackpressureThreshold = options.BackpressureThreshold,
                Mode = options.Mode,
                MaxProducerWaitTime = options.MaxProducerWaitTime,
                AdaptiveAdjustmentInterval = options.AdaptiveAdjustmentInterval
            };
        }
    }
}
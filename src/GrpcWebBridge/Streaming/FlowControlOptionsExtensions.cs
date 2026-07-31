using System;
using System.Threading;
using System.Threading.Channels;

namespace GrpcWebBridge.Streaming
{
    public static class FlowControlOptionsExtensions
    {
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
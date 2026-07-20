#nullable enable
using System;
using Microsoft.AspNetCore.Mvc;
using GrpcWebBridge.Streaming;

namespace GrpcWebBridge.Controllers
{
    /// <summary>
    /// Exposes streaming diagnostics via HTTP GET.
    /// </summary>
    [ApiController]
    [Route("api/streams")]
    public sealed class StreamDiagnosticsController : ControllerBase
    {
        private readonly IBidirectionalStreamingEngine _engine;
        private const double DefaultBackpressureWarnThreshold = 0.10; // matches StreamDiagnosticsOptions default

        public StreamDiagnosticsController(IBidirectionalStreamingEngine engine)
        {
            _engine = engine ?? throw new ArgumentNullException(nameof(engine));
        }

        /// <summary>
        /// Returns an aggregate snapshot of all active bidirectional streams.
        /// </summary>
        /// <remarks>
        /// The shape mirrors <see cref="StreamingDiagnosticsEvent"/> but is returned as JSON.
        /// </remarks>
        [HttpGet("diagnostics")]
        public IActionResult GetDiagnostics()
        {
            var allMetrics = _engine.GetAllMetrics();
            int activeCount = allMetrics.Count;

            long totalIn = 0, totalOut = 0, totalBytesIn = 0, totalBytesOut = 0;
            long totalBackpressure = 0, totalCreditWaitMs = 0;
            int zeroActivity = 0, highBackpressure = 0;

            foreach (var (streamId, metrics) in allMetrics)
            {
                long messageTotal = metrics.MessagesIn + metrics.MessagesOut;

                totalIn += metrics.MessagesIn;
                totalOut += metrics.MessagesOut;
                totalBytesIn += metrics.BytesIn;
                totalBytesOut += metrics.BytesOut;
                totalBackpressure += metrics.BackpressureEvents;
                totalCreditWaitMs += metrics.TotalCreditWaitMs;

                // High backpressure detection (same logic as the background service)
                if (messageTotal > 0)
                {
                    double ratio = (double)metrics.BackpressureEvents / messageTotal;
                    if (ratio > DefaultBackpressureWarnThreshold)
                    {
                        highBackpressure++;
                    }
                }

                // Zero‑activity detection – streams with no inbound or outbound messages
                if (metrics.MessagesIn == 0 && metrics.MessagesOut == 0)
                {
                    zeroActivity++;
                }
            }

            var result = new
            {
                ActiveStreamCount = activeCount,
                TotalMessagesIn = totalIn,
                TotalMessagesOut = totalOut,
                TotalBytesIn = totalBytesIn,
                TotalBytesOut = totalBytesOut,
                TotalBackpressureEvents = totalBackpressure,
                TotalCreditWaitMs = totalCreditWaitMs,
                ZeroActivityStreamCount = zeroActivity,
                HighBackpressureStreamCount = highBackpressure
            };

            return Ok(result);
        }
    }
}

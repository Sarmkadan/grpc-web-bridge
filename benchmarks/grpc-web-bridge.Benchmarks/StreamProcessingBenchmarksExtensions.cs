using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace GrpcWebBridge.Benchmarks
{
    /// <summary>
    /// Extension methods that provide additional diagnostics for <see cref="StreamProcessingBenchmarks"/>.
    /// </summary>
    public static class StreamProcessingBenchmarksExtensions
    {
        /// <summary>
        /// Measures the read‑throughput of the 1 KB benchmark in kilobytes per second.
        /// </summary>
        /// <param name="benchmarks">The benchmark instance.</param>
        /// <returns>The throughput expressed as kilobytes per second.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="benchmarks"/> is <c>null</c>.</exception>
        public static async Task<double> MeasureReadThroughput1KBAsync(this StreamProcessingBenchmarks benchmarks)
        {
            ArgumentNullException.ThrowIfNull(benchmarks);

            var stopwatch = Stopwatch.StartNew();
            var data = await benchmarks.ReadStreamToEnd_1KB().ConfigureAwait(false);
            stopwatch.Stop();

            // data.Length is the number of bytes read (should be 1 024).
            return (data.Length / 1024.0) / stopwatch.Elapsed.TotalSeconds;
        }

        /// <summary>
        /// Measures the read‑throughput of the 64 KB benchmark in kilobytes per second.
        /// </summary>
        /// <param name="benchmarks">The benchmark instance.</param>
        /// <returns>The throughput expressed as kilobytes per second.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="benchmarks"/> is <c>null</c>.</exception>
        public static async Task<double> MeasureReadThroughput64KBAsync(this StreamProcessingBenchmarks benchmarks)
        {
            ArgumentNullException.ThrowIfNull(benchmarks);

            var stopwatch = Stopwatch.StartNew();
            var data = await benchmarks.ReadStreamToEnd_64KB().ConfigureAwait(false);
            stopwatch.Stop();

            return (data.Length / 1024.0) / stopwatch.Elapsed.TotalSeconds;
        }

        /// <summary>
        /// Returns the length of the Base64 string produced by the 1 KB benchmark.
        /// </summary>
        /// <param name="benchmarks">The benchmark instance.</param>
        /// <returns>The length of the Base64‑encoded string.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="benchmarks"/> is <c>null</c>.</exception>
        public static async Task<int> GetBase64Length1KBAsync(this StreamProcessingBenchmarks benchmarks)
        {
            ArgumentNullException.ThrowIfNull(benchmarks);

            var base64 = await benchmarks.StreamToBase64_1KB().ConfigureAwait(false);
            return base64.Length;
        }

        /// <summary>
        /// Executes the chunked copy benchmark for the specified size and returns the elapsed time in milliseconds.
        /// </summary>
        /// <param name="benchmarks">The benchmark instance.</param>
        /// <param name="size">The size identifier: <c>1KB</c>, <c>64KB</c> or <c>1MB</c>.</param>
        /// <returns>The elapsed time in milliseconds.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="benchmarks"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="size"/> is not a recognised identifier.</exception>
        public static async Task<double> MeasureCopyChunkedAsync(this StreamProcessingBenchmarks benchmarks, string size)
        {
            ArgumentNullException.ThrowIfNull(benchmarks);
            ArgumentException.ThrowIfNullOrEmpty(size);

            var stopwatch = Stopwatch.StartNew();

            _ = size switch
            {
                "1KB" => benchmarks.CopyStreamChunked_1KB(),
                "64KB" => benchmarks.CopyStreamChunked_64KB(),
                "1MB" => benchmarks.CopyStreamChunked_1MB(),
                _ => throw new ArgumentException($"Unsupported size identifier '{size}'. Use '1KB', '64KB' or '1MB'.", nameof(size))
            };

            // Await the selected task to ensure completion.
            await (size switch
            {
                "1KB" => benchmarks.CopyStreamChunked_1KB(),
                "64KB" => benchmarks.CopyStreamChunked_64KB(),
                "1MB" => benchmarks.CopyStreamChunked_1MB(),
                _ => Task.CompletedTask
            }).ConfigureAwait(false);

            stopwatch.Stop();
            return stopwatch.Elapsed.TotalMilliseconds;
        }
    }
}

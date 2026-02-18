using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace GrpcWebBridge.Benchmarks
{
    /// <summary>
    /// Extension methods for <see cref="JsonUtilityBenchmarks"/> to provide utility 
    /// and introspection capabilities outside of the standard BenchmarkDotNet execution.
    /// </summary>
    public static class JsonUtilityBenchmarksExtensions
    {
        /// <summary>
        /// Gets the size of the serialized JSON string in bytes.
        /// </summary>
        /// <param name="benchmarks">The benchmark instance.</param>
        /// <exception cref="ArgumentNullException"><paramref name="benchmarks"/> is <see langword="null"/>.</exception>
        /// <returns>The length of the serialized string.</returns>
        public static int GetSerializedSize(this JsonUtilityBenchmarks benchmarks)
        {
            ArgumentNullException.ThrowIfNull(benchmarks);
            return benchmarks.Serialize().Length;
        }

        /// <summary>
        /// Gets the number of keys in the deserialized dictionary.
        /// </summary>
        /// <param name="benchmarks">The benchmark instance.</param>
        /// <exception cref="ArgumentNullException"><paramref name="benchmarks"/> is <see langword="null"/>.</exception>
        /// <returns>The count of keys in the dictionary, or 0 if deserialization returns null.</returns>
        public static int GetDeserializedDictionaryCount(this JsonUtilityBenchmarks benchmarks)
        {
            ArgumentNullException.ThrowIfNull(benchmarks);
            var dictionary = benchmarks.DeserializeToDictionary();
            return dictionary?.Count ?? 0;
        }

        /// <summary>
        /// Measures the time taken to perform a single serialization operation.
        /// </summary>
        /// <param name="benchmarks">The benchmark instance.</param>
        /// <exception cref="ArgumentNullException"><paramref name="benchmarks"/> is <see langword="null"/>.</exception>
        /// <returns>The time taken to serialize the object.</returns>
        public static TimeSpan MeasureSerializationTime(this JsonUtilityBenchmarks benchmarks)
        {
            ArgumentNullException.ThrowIfNull(benchmarks);
            var stopwatch = Stopwatch.StartNew();
            benchmarks.Serialize();
            stopwatch.Stop();
            return stopwatch.Elapsed;
        }
    }
}

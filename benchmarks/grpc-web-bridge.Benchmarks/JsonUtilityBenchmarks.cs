#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using GrpcWebBridge.Utilities;

namespace GrpcWebBridge.Benchmarks;

/// <summary>
/// Benchmark class for JsonUtility.
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public sealed class JsonUtilityBenchmarks
{
    private string _sampleJson = null!;
    private object _sampleObject = null!;

    /// <summary>
    /// Initializes the benchmark.
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        _sampleObject = new { Name = "Test", Value = 42, Timestamp = DateTime.UtcNow };
        _sampleJson = "{\"name\":\"Test\",\"value\":42,\"timestamp\":\"2026-07-02T12:00:00Z\"}";
    }

    /// <summary>
    /// Serializes a simple object to JSON.
    /// </summary>
    /// <returns>The serialized JSON string.</returns>
    [Benchmark(Description = "Serialize — Simple Object")]
    public string Serialize() => JsonUtility.Serialize(_sampleObject);

    /// <summary>
    /// Deserializes a simple object from JSON.
    /// </summary>
    /// <returns>The deserialized object.</returns>
    [Benchmark(Description = "Deserialize — Simple Object")]
    public object? Deserialize() => JsonUtility.Deserialize<object>(_sampleJson);

    /// <summary>
    /// Deserializes a simple object from JSON to a dictionary.
    /// </summary>
    /// <returns>The deserialized dictionary.</returns>
    [Benchmark(Description = "DeserializeToDictionary — Simple Object")]
    public Dictionary<string, object>? DeserializeToDictionary() => JsonUtility.DeserializeToDictionary(_sampleJson);
}

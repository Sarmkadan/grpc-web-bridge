#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using GrpcWebBridge.Utilities;

namespace GrpcWebBridge.Benchmarks;

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public sealed class JsonUtilityBenchmarks
{
    private string _sampleJson = null!;
    private object _sampleObject = null!;

    [GlobalSetup]
    public void Setup()
    {
        _sampleObject = new { Name = "Test", Value = 42, Timestamp = DateTime.UtcNow };
        _sampleJson = "{\"name\":\"Test\",\"value\":42,\"timestamp\":\"2026-07-02T12:00:00Z\"}";
    }

    [Benchmark(Description = "Serialize — Simple Object")]
    public string Serialize() => JsonUtility.Serialize(_sampleObject);

    [Benchmark(Description = "Deserialize — Simple Object")]
    public object? Deserialize() => JsonUtility.Deserialize<object>(_sampleJson);

    [Benchmark(Description = "DeserializeToDictionary — Simple Object")]
    public Dictionary<string, object>? DeserializeToDictionary() => JsonUtility.DeserializeToDictionary(_sampleJson);
}

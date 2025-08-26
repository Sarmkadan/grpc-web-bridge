// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using GrpcWebBridge.Domain;
using GrpcWebBridge.Domain.Models;
using GrpcWebBridge.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace GrpcWebBridge.Benchmarks;

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class ProtocolTranslationBenchmarks
{
    private ProtocolTranslationService _service = null!;

    private Dictionary<string, string> _smallMetadata = null!;
    private Dictionary<string, string> _largeMetadata = null!;
    private byte[] _jsonPayload = null!;
    private byte[] _base64JsonPayload = null!;
    private byte[] _protobufPayload = null!;
    private GrpcResponse _protobufResponse = null!;
    private GrpcResponse _jsonResponse = null!;

    [GlobalSetup]
    public void Setup()
    {
        _service = new ProtocolTranslationService(NullLogger<ProtocolTranslationService>.Instance);

        _smallMetadata = new Dictionary<string, string>
        {
            ["Content-Type"] = "application/grpc-web",
            ["Authorization"] = "Bearer token123",
            ["X-Request-ID"] = "req-abc-123",
            ["X-Trace-ID"] = "trace-xyz-789",
            ["Accept-Encoding"] = "gzip"
        };

        _largeMetadata = Enumerable.Range(0, 50)
            .ToDictionary(i => $"X-Custom-Header-{i}", i => $"value-{i}-data");

        _protobufPayload = new byte[256];
        new Random(42).NextBytes(_protobufPayload);

        var base64 = Convert.ToBase64String(_protobufPayload);
        var jsonStr = $"{{\"data\":\"{base64}\"}}";
        _base64JsonPayload = System.Text.Encoding.UTF8.GetBytes(jsonStr);

        _jsonPayload = System.Text.Encoding.UTF8.GetBytes("{\"name\":\"test\",\"value\":42}");

        var requestId = Guid.NewGuid().ToString("N");
        _protobufResponse = new GrpcResponse(requestId, _protobufPayload)
        {
            Status = GrpcStatusCode.Ok,
            StatusMessage = "OK",
            PayloadFormat = SerializationFormat.Protobuf
        };

        _jsonResponse = new GrpcResponse(requestId, _jsonPayload)
        {
            Status = GrpcStatusCode.Ok,
            StatusMessage = "OK",
            PayloadFormat = SerializationFormat.Json
        };
    }

    [Benchmark(Description = "TranslateMetadata — 5 headers")]
    public Dictionary<string, string> TranslateMetadata_Small() =>
        _service.TranslateMetadata(_smallMetadata);

    [Benchmark(Description = "TranslateMetadata — 50 headers")]
    public Dictionary<string, string> TranslateMetadata_Large() =>
        _service.TranslateMetadata(_largeMetadata);

    [Benchmark(Description = "ConvertProtobufToJson — 256 B payload")]
    public byte[] ConvertProtobufToJson_256B() =>
        _service.ConvertProtobufToJson(_protobufPayload);

    [Benchmark(Description = "ConvertJsonToProtobuf — base64-wrapped payload")]
    public byte[] ConvertJsonToProtobuf_Base64() =>
        _service.ConvertJsonToProtobuf(_base64JsonPayload);

    [Benchmark(Description = "TranslateGrpcToHttp — Protobuf passthrough")]
    public byte[] TranslateGrpcToHttp_Passthrough() =>
        _service.TranslateGrpcToHttp(_protobufResponse, SerializationFormat.Protobuf);

    [Benchmark(Description = "TranslateGrpcToHttp — Protobuf→Json")]
    public byte[] TranslateGrpcToHttp_Convert() =>
        _service.TranslateGrpcToHttp(_protobufResponse, SerializationFormat.Json);
}

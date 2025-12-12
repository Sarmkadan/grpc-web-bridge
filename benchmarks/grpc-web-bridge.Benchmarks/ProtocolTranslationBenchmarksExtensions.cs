#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Text;
using GrpcWebBridge.Domain;
using GrpcWebBridge.Domain.Models;

namespace GrpcWebBridge.Benchmarks;

public static class ProtocolTranslationBenchmarksExtensions
{
    /// <summary>
    /// Creates a pre-configured benchmark instance with common test data already initialized.
    /// </summary>
    /// <param name="benchmark">The benchmark instance to configure</param>
    /// <returns>The configured benchmark instance for fluent chaining</returns>
    public static ProtocolTranslationBenchmarks WithPreconfiguredData(this ProtocolTranslationBenchmarks benchmark)
    {
        benchmark.Setup();
        return benchmark;
    }

    /// <summary>
    /// Executes all metadata translation benchmarks and returns a dictionary of results.
    /// Useful for testing different metadata sizes in one call.
    /// </summary>
    /// <param name="benchmark">The benchmark instance</param>
    /// <param name="smallHeaders">Whether to use small metadata (5 headers) or large (50 headers)</param>
    /// <returns>Dictionary with benchmark names and their results</returns>
    public static Dictionary<string, Dictionary<string, string>> TranslateAllMetadata(
        this ProtocolTranslationBenchmarks benchmark,
        bool smallHeaders = true)
    {
        var results = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

        if (smallHeaders)
        {
            var result = benchmark.TranslateMetadata_Small();
            results["TranslateMetadata_Small"] = result;
        }
        else
        {
            var result = benchmark.TranslateMetadata_Large();
            results["TranslateMetadata_Large"] = result;
        }

        return results;
    }

    /// <summary>
    /// Converts protobuf to JSON and returns the result as a UTF-8 string.
    /// Convenience method for easier result inspection in tests.
    /// </summary>
    /// <param name="benchmark">The benchmark instance</param>
    /// <param name="protobufData">The protobuf data to convert</param>
    /// <returns>The JSON string representation</returns>
    public static string ConvertProtobufToJsonString(
        this ProtocolTranslationBenchmarks benchmark,
        byte[] protobufData
    )
    {
        var jsonBytes = benchmark.ConvertProtobufToJson_256B();
        return Encoding.UTF8.GetString(jsonBytes);
    }

    /// <summary>
    /// Converts base64 JSON to protobuf and returns the raw protobuf bytes.
    /// Convenience method that wraps the base64 handling.
    /// </summary>
    /// <param name="benchmark">The benchmark instance</param>
    /// <param name="base64JsonData">Base64-encoded JSON data</param>
    /// <returns>The converted protobuf bytes</returns>
    public static byte[] ConvertBase64JsonToProtobuf(
        this ProtocolTranslationBenchmarks benchmark,
        byte[] base64JsonData
    )
    {
        return benchmark.ConvertJsonToProtobuf_Base64();
    }

    /// <summary>
    /// Translates gRPC response to HTTP format with automatic format detection.
    /// Determines the appropriate format based on the response's payload format.
    /// </summary>
    /// <param name="benchmark">The benchmark instance</param>
    /// <param name="response">The gRPC response to translate</param>
    /// <returns>The translated HTTP payload</returns>
    public static byte[] TranslateGrpcToHttpAuto(
        this ProtocolTranslationBenchmarks benchmark,
        GrpcResponse response
    )
    {
        return response.PayloadFormat == SerializationFormat.Json
            ? benchmark.TranslateGrpcToHttp_Passthrough()
            : benchmark.TranslateGrpcToHttp_Convert();
    }

    /// <summary>
    /// Creates a test gRPC response with the specified payload.
    /// Helper method to generate test data inline.
    /// </summary>
    /// <param name="benchmark">The benchmark instance</param>
    /// <param name="payload">The payload bytes</param>
    /// <param name="format">The serialization format</param>
    /// <returns>A configured GrpcResponse instance</returns>
    public static GrpcResponse CreateTestResponse(
        this ProtocolTranslationBenchmarks benchmark,
        byte[] payload,
        SerializationFormat format = SerializationFormat.Protobuf
    )
    {
        var requestId = Guid.NewGuid().ToString("N");
        return new GrpcResponse(requestId, payload)
        {
            Status = GrpcStatusCode.Ok,
            StatusMessage = "OK",
            PayloadFormat = format
        };
    }
}
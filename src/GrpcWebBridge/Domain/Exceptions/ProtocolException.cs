// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace GrpcWebBridge.Domain.Exceptions;

/// <summary>
/// Exception thrown during protocol translation and conversion
/// </summary>
public class ProtocolException : GrpcWebBridgeException
{
    public string? SourceFormat { get; set; }
    public string? TargetFormat { get; set; }
    public string? RequestId { get; set; }

    public ProtocolException() : base() { }

    public ProtocolException(string message) : base(message, "PROTOCOL_ERROR") { }

    public ProtocolException(string message, Exception? innerException)
        : base(message, innerException)
    {
        ErrorCode = "PROTOCOL_ERROR";
    }

    public ProtocolException(string sourceFormat, string targetFormat, string message)
        : base($"Protocol translation from {sourceFormat} to {targetFormat} failed: {message}", "TRANSLATION_FAILED")
    {
        SourceFormat = sourceFormat;
        TargetFormat = targetFormat;
        GrpcStatus = GrpcStatusCode.InvalidArgument;
    }

    public override string ToString()
    {
        var result = base.ToString();
        if (!string.IsNullOrEmpty(RequestId))
            result += $" | Request: {RequestId}";

        if (!string.IsNullOrEmpty(SourceFormat) || !string.IsNullOrEmpty(TargetFormat))
            result += $" | Conversion: {SourceFormat} -> {TargetFormat}";

        return result;
    }
}

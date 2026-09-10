#nullable enable
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
    /// <summary>
    /// Gets or sets the source format.
    /// </summary>
    public string? SourceFormat { get; set; }
    /// <summary>
    /// Gets or sets the target format.
    /// </summary>
    public string? TargetFormat { get; set; }
    /// <summary>
    /// Gets or sets the request identifier.
    /// </summary>
    public string? RequestId { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProtocolException"/> class.
    /// </summary>
    public ProtocolException() : base() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProtocolException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    public ProtocolException(string message) : base(message, "PROTOCOL_ERROR") { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProtocolException"/> class with a specified error message and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception, or a null reference if no inner exception is specified.</param>
    public ProtocolException(string message, Exception? innerException)
        : base(message, innerException)
    {
        ErrorCode = "PROTOCOL_ERROR";
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProtocolException"/> class for a protocol translation error.
    /// </summary>
    /// <param name="sourceFormat">The source format that was being translated from.</param>
    /// <param name="targetFormat">The target format that was being translated to.</param>
    /// <param name="message">The error message that explains the reason for the exception.</param>
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

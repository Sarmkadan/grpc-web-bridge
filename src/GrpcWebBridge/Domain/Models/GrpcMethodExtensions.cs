#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System.Text;
using GrpcWebBridge.Domain.Models;

namespace GrpcWebBridge.Domain.Models;

/// <summary>
/// Extension methods for <see cref="GrpcMethod"/> providing useful utility functionality
/// </summary>
public static class GrpcMethodExtensions
{
    /// <summary>
    /// Generates a C# method signature string for this gRPC method
    /// </summary>
    /// <param name="method">The gRPC method</param>
    /// <param name="includeAsync">Whether to include async modifier</param>
    /// <param name="includeCancellationToken">Whether to include CancellationToken parameter</param>
    /// <returns>C# method signature as string</returns>
    public static string ToCSharpSignature(this GrpcMethod method, bool includeAsync = true, bool includeCancellationToken = true)
    {
        if (method is null)
            throw new ArgumentNullException(nameof(method));

        var builder = new StringBuilder();

        // Access modifiers
        builder.Append("public ");

        // Async modifier
        if (includeAsync)
            builder.Append("async ");

        // Return type
        if (method.Type == MethodType.ServerStreaming || method.Type == MethodType.BidirectionalStreaming)
            builder.Append("IAsyncEnumerable<");
        else
            builder.Append("Task<");

        builder.Append(method.OutputMessageType);
        builder.Append("> ");

        // Method name
        builder.Append(method.Name);
        builder.Append("Async");

        // Parameters
        builder.Append("(");

        // Input message parameter
        builder.Append(method.InputMessageType);
        builder.Append(" request");

        // CancellationToken parameter
        if (includeCancellationToken)
        {
            if (method.InputParameters.Count > 0 || method.OutputParameters.Count > 0)
                builder.Append(", ");
            builder.Append("CancellationToken cancellationToken = default");
        }

        builder.Append(")");

        return builder.ToString();
    }

    /// <summary>
    /// Checks if this gRPC method is a streaming method (either server or bidirectional)
    /// </summary>
    /// <param name="method">The gRPC method</param>
    /// <returns>True if method is streaming, false otherwise</returns>
    public static bool IsStreaming(this GrpcMethod method)
    {
        if (method is null)
            throw new ArgumentNullException(nameof(method));

        return method.Type == MethodType.ServerStreaming || method.Type == MethodType.BidirectionalStreaming;
    }

    /// <summary>
    /// Gets the total number of parameters (input + output) for this gRPC method
    /// </summary>
    /// <param name="method">The gRPC method</param>
    /// <returns>Total parameter count</returns>
    public static int GetTotalParameterCount(this GrpcMethod method)
    {
        if (method is null)
            throw new ArgumentNullException(nameof(method));

        return method.InputParameters.Count + method.OutputParameters.Count;
    }

    /// <summary>
    /// Generates a summary comment for this gRPC method based on its properties
    /// </summary>
    /// <param name="method">The gRPC method</param>
    /// <returns>XML documentation comment as string</returns>
    public static string ToXmlDocumentation(this GrpcMethod method)
    {
        if (method is null)
            throw new ArgumentNullException(nameof(method));

        var builder = new StringBuilder();
        builder.AppendLine("/// <summary>");

        if (!string.IsNullOrWhiteSpace(method.Description))
        {
            builder.AppendLine($"/// {method.Description}");
        }
        else
        {
            builder.AppendLine($"/// {method.FullName} - {method.Type} method");
        }

        builder.AppendLine("/// </summary>");

        if (method.InputParameters.Count > 0)
        {
            builder.AppendLine("/// <param name=\"request\">Request message</param>");
        }

        builder.AppendLine("/// <returns>Response message</returns>");

        if (method.IsDeprecated)
        {
            builder.AppendLine("/// <remarks>This method is deprecated</remarks>");
        }

        if (method.TimeoutMilliseconds > 0 && method.TimeoutMilliseconds != Constants.Grpc.DefaultTimeout)
        {
            builder.AppendLine($"/// <remarks>Timeout: {method.TimeoutMilliseconds}ms</remarks>");
        }

        return builder.ToString();
    }
}
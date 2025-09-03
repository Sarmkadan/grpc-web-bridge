// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Collections.ObjectModel;

namespace GrpcWebBridge.Domain.Models;

/// <summary>
/// Represents a single gRPC method definition with full metadata
/// </summary>
public class GrpcMethod
{
    private readonly List<MethodParameter> _inputParameters = [];
    private readonly List<MethodParameter> _outputParameters = [];

    public string Name { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public MethodType Type { get; set; } = MethodType.Unary;
    public string InputMessageType { get; set; } = string.Empty;
    public string OutputMessageType { get; set; } = string.Empty;
    public bool IsDeprecated { get; set; }
    public string? Description { get; set; }
    public int TimeoutMilliseconds { get; set; } = Constants.Grpc.DefaultTimeout;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public IReadOnlyCollection<MethodParameter> InputParameters => _inputParameters.AsReadOnly();
    public IReadOnlyCollection<MethodParameter> OutputParameters => _outputParameters.AsReadOnly();

    public GrpcMethod() { }

    public GrpcMethod(string name, string fullName, MethodType type, string inputMessage, string outputMessage)
    {
        Name = ValidateName(name);
        FullName = ValidateFullName(fullName);
        Type = type;
        InputMessageType = ValidateMessageType(inputMessage);
        OutputMessageType = ValidateMessageType(outputMessage);
    }

    public void AddInputParameter(MethodParameter parameter)
    {
        if (parameter is null)
            throw new ArgumentNullException(nameof(parameter));

        parameter.Validate();

        if (_inputParameters.Any(p => p.Name == parameter.Name))
            throw new InvalidOperationException($"Parameter '{parameter.Name}' already exists");

        _inputParameters.Add(parameter);
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddOutputParameter(MethodParameter parameter)
    {
        if (parameter is null)
            throw new ArgumentNullException(nameof(parameter));

        parameter.Validate();

        if (_outputParameters.Any(p => p.Name == parameter.Name))
            throw new InvalidOperationException($"Parameter '{parameter.Name}' already exists");

        _outputParameters.Add(parameter);
        UpdatedAt = DateTime.UtcNow;
    }

    public void RemoveInputParameter(string parameterName)
    {
        var parameter = _inputParameters.FirstOrDefault(p => p.Name == parameterName);
        if (parameter is not null)
        {
            _inputParameters.Remove(parameter);
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Name))
            throw new ArgumentException("Method name cannot be empty", nameof(Name));

        if (string.IsNullOrWhiteSpace(FullName))
            throw new ArgumentException("Method full name cannot be empty", nameof(FullName));

        if (string.IsNullOrWhiteSpace(InputMessageType))
            throw new ArgumentException("Input message type cannot be empty", nameof(InputMessageType));

        if (string.IsNullOrWhiteSpace(OutputMessageType))
            throw new ArgumentException("Output message type cannot be empty", nameof(OutputMessageType));

        if (TimeoutMilliseconds <= 0)
            throw new ArgumentException("Timeout must be greater than 0", nameof(TimeoutMilliseconds));
    }

    private static string ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Method name cannot be empty", nameof(name));
        return name.Trim();
    }

    private static string ValidateFullName(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            throw new ArgumentException("Full name cannot be empty", nameof(fullName));
        return fullName.Trim();
    }

    private static string ValidateMessageType(string messageType)
    {
        if (string.IsNullOrWhiteSpace(messageType))
            throw new ArgumentException("Message type cannot be empty", nameof(messageType));
        return messageType.Trim();
    }

    public override string ToString() => $"{FullName} ({Type})";

    public override bool Equals(object? obj)
    {
        if (obj is not GrpcMethod other)
            return false;

        return FullName == other.FullName && Type == other.Type;
    }

    public override int GetHashCode() => HashCode.Combine(FullName, Type);
}

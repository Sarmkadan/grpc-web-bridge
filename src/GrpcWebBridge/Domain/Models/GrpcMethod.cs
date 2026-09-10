#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Collections.ObjectModel;

namespace GrpcWebBridge.Domain.Models;

/// <summary>
/// Represents a single gRPC method definition with full metadata
/// </summary>
public sealed class GrpcMethod
{
    private readonly List<MethodParameter> _inputParameters = [];
    private readonly List<MethodParameter> _outputParameters = [];

    /// <summary>
    /// Gets or sets the name of the method.
    /// </summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the full name of the method.
    /// </summary>
    public string FullName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the type of the method (e.g., Unary, ServerStreaming, etc.).
    /// </summary>
    public MethodType Type { get; set; } = MethodType.Unary;
    /// <summary>
    /// Gets or sets the input message type.
    /// </summary>
    public string InputMessageType { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the output message type.
    /// </summary>
    public string OutputMessageType { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets a value indicating whether the method is deprecated.
    /// </summary>
    public bool IsDeprecated { get; set; }
    /// <summary>
    /// Gets or sets the description of the method.
    /// </summary>
    public string? Description { get; set; }
    /// <summary>
    /// Gets or sets the timeout in milliseconds for the method.
    /// </summary>
    public int TimeoutMilliseconds { get; set; } = Constants.Grpc.DefaultTimeout;
    /// <summary>
    /// Gets or sets the date and time when the method was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    /// <summary>
    /// Gets or sets the date and time when the method was last updated.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Gets the collection of input parameters for the method.
    /// </summary>
    public IReadOnlyCollection<MethodParameter> InputParameters => _inputParameters.AsReadOnly();
    /// <summary>
    /// Gets the collection of output parameters for the method.
    /// </summary>
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

    /// <summary>
    /// Adds an input parameter to the method.
    /// </summary>
    /// <param name="parameter">The parameter to add.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="parameter"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when a parameter with the same name already exists.</exception>
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

    /// <summary>
    /// Adds an output parameter to the method.
    /// </summary>
    /// <param name="parameter">The parameter to add.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="parameter"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when a parameter with the same name already exists.</exception>
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

    /// <summary>
    /// Removes the input parameter with the specified name from the method.
    /// </summary>
    /// <param name="parameterName">The name of the parameter to remove.</param>
    /// <remarks>If the parameter does not exist, this method does nothing.</remarks>
    public void RemoveInputParameter(string parameterName)
    {
        var parameter = _inputParameters.FirstOrDefault(p => p.Name == parameterName);
        if (parameter is not null)
        {
            _inputParameters.Remove(parameter);
            UpdatedAt = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Validates the method's properties.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when the method name, full name, input message type, output message type is empty or whitespace, or when the timeout is less than or equal to zero.</exception>
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

    /// <summary>
    /// Determines whether the specified object is equal to the current method.
    /// </summary>
    /// <param name="obj">The object to compare with the current method.</param>
    /// <returns>true if the specified object is equal to the current method; otherwise, false.</returns>
    public override bool Equals(object? obj)
    {
        if (obj is not GrpcMethod other)
            return false;

        return FullName == other.FullName && Type == other.Type;
    }

    /// <summary>
    /// Serves as the default hash function.
    /// </summary>
    /// <returns>A hash code for the current method.</returns>
    public override int GetHashCode() => HashCode.Combine(FullName, Type);
}
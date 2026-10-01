#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Collections.Concurrent;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using GrpcWebBridge.Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace GrpcWebBridge.Services;

/// <summary>
/// Registry for protobuf message descriptors and file descriptors.
/// Enables dynamic message parsing and reflection-based serialization
/// for gRPC-Web bridge operations without compile-time proto knowledge.
/// </summary>
public sealed class ProtoRegistry
{
    private readonly ILogger<ProtoRegistry> _logger;
    private readonly ConcurrentDictionary<string, FileDescriptor> _fileDescriptors = new();
    private readonly ConcurrentDictionary<string, MessageDescriptor> _messageDescriptors = new();
    private readonly ConcurrentDictionary<string, Google.Protobuf.Reflection.ServiceDescriptor> _serviceDescriptors = new();
    private readonly object _registrationLock = new();

    /// <summary>
    /// Gets the total number of registered file descriptors.
    /// </summary>
    public int FileDescriptorCount => _fileDescriptors.Count;

    /// <summary>
    /// Gets the total number of registered message types.
    /// </summary>
    public int MessageTypeCount => _messageDescriptors.Count;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProtoRegistry"/> class.
    /// </summary>
    /// <param name="logger">The logger for recording registry operations.</param>
    public ProtoRegistry(ILogger<ProtoRegistry> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Registers a file descriptor and indexes all its message types and services.
    /// </summary>
    /// <param name="fileDescriptor">The protobuf file descriptor to register.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="fileDescriptor"/> is null.</exception>
    public void RegisterFileDescriptor(FileDescriptor fileDescriptor)
    {
        ArgumentNullException.ThrowIfNull(fileDescriptor);

        lock (_registrationLock)
        {
            var fullName = fileDescriptor.Name;
            if (_fileDescriptors.ContainsKey(fullName))
            {
                _logger.LogDebug("File descriptor '{FileName}' already registered, skipping", fullName);
                return;
            }

            _fileDescriptors[fullName] = fileDescriptor;

            foreach (var messageType in fileDescriptor.MessageTypes)
            {
                IndexMessageDescriptor(messageType);
            }

            foreach (var service in fileDescriptor.Services)
            {
                _serviceDescriptors[service.FullName] = service;
                _logger.LogDebug("Indexed service descriptor: {ServiceName}", service.FullName);
            }

            _logger.LogInformation(
                "Registered file descriptor '{FileName}' with {MessageCount} message types and {ServiceCount} services",
                fullName, fileDescriptor.MessageTypes.Count, fileDescriptor.Services.Count);
        }
    }

    /// <summary>
    /// Resolves a message descriptor by its fully qualified protobuf type name.
    /// </summary>
    /// <param name="fullTypeName">The fully qualified type name (e.g., "package.MessageName").</param>
    /// <returns>The resolved message descriptor.</returns>
    /// <exception cref="ConfigurationException">Thrown when the type is not found in the registry.</exception>
    public MessageDescriptor ResolveMessageType(string fullTypeName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fullTypeName);

        if (_messageDescriptors.TryGetValue(fullTypeName, out var descriptor))
        {
            return descriptor;
        }

        _logger.LogError("Message type '{TypeName}' not found in proto registry", fullTypeName);
        throw new ConfigurationException($"Unknown protobuf message type: {fullTypeName}");
    }

    /// <summary>
    /// Attempts to resolve a message descriptor without throwing.
    /// </summary>
    /// <param name="fullTypeName">The fully qualified type name.</param>
    /// <param name="descriptor">The resolved descriptor, or null if not found.</param>
    /// <returns>True if the type was found; otherwise false.</returns>
    public bool TryResolveMessageType(string fullTypeName, out MessageDescriptor? descriptor)
    {
        if (string.IsNullOrWhiteSpace(fullTypeName))
        {
            descriptor = null;
            return false;
        }

        return _messageDescriptors.TryGetValue(fullTypeName, out descriptor);
    }

    /// <summary>
    /// Returns all registered service descriptors.
    /// </summary>
    /// <returns>A read-only collection of service descriptors.</returns>
    public IReadOnlyCollection<Google.Protobuf.Reflection.ServiceDescriptor> GetRegisteredServices()
    {
        return _serviceDescriptors.Values.ToList().AsReadOnly();
    }

    private void IndexMessageDescriptor(MessageDescriptor messageDescriptor)
    {
        _messageDescriptors[messageDescriptor.FullName] = messageDescriptor;
        _logger.LogDebug("Indexed message descriptor: {MessageName}", messageDescriptor.FullName);

        foreach (var nested in messageDescriptor.NestedTypes)
        {
            IndexMessageDescriptor(nested);
        }
    }
}

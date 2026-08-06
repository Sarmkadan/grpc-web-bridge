#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Collections.Concurrent;

namespace GrpcWebBridge.Integration;

/// <summary>
/// Manages request context across async operations.
/// Provides ambient context for request-scoped data without explicit parameter passing.
/// Enables correlation logging and cross-cutting concerns.
/// </summary>
public sealed class RequestContextManager : IDisposable
{
    private static readonly AsyncLocal<RequestContext> _context = new();
    private static readonly ConcurrentDictionary<string, RequestContext> _activeContexts = new();
    private static readonly object _registryLock = new();
    private readonly ILogger<RequestContextManager> _logger;
    private int _disposed;

    public RequestContextManager(ILogger<RequestContextManager> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
    }

    /// <summary>
    /// Sanitizes a metadata key by removing control characters and validating length.
    /// </summary>
    /// <param name="key">The metadata key to sanitize.</param>
    /// <returns>The sanitized key, or null if invalid.</returns>
    private string? SanitizeMetadataKey(string key)
    {
        if (string.IsNullOrEmpty(key))
            return null;

        // Remove control characters (0x00-0x1F, 0x7F-0x9F)
        var sanitized = new char[key.Length];
        var length = 0;
        for (var i = 0; i < key.Length; i++)
        {
            var c = key[i];
            if (c >= ' ' && c <= '~') // Printable ASCII
            {
                sanitized[length++] = c;
            }
        }

        var result = new string(sanitized, 0, length);

        // Validate length
        if (result.Length > RequestContext.MaxMetadataKeyLength)
        {
            _logger.LogWarning("Metadata key '{OriginalKey}' exceeds maximum length of {MaxLength} characters",
                key, RequestContext.MaxMetadataKeyLength);
            return null;
        }

        return result.Length == 0 ? null : result;
    }

    /// <summary>
    /// Sanitizes a metadata value by removing control characters, newlines, and validating length.
    /// </summary>
    /// <param name="value">The metadata value to sanitize.</param>
    /// <param name="truncated">Receives true if the value was truncated.</param>
    /// <returns>The sanitized value, or null if invalid.</returns>
    private string? SanitizeMetadataValue(string value, out bool truncated)
    {
        truncated = false;
        if (string.IsNullOrEmpty(value))
            return value;

        // Remove control characters and newlines to prevent log injection
        var sanitized = new char[value.Length];
        var length = 0;
        var wasTruncated = false;

        for (var i = 0; i < value.Length; i++)
        {
            var c = value[i];

            // Allow printable ASCII and common Unicode characters, but remove control characters
            if (c >= ' ' && c <= '~')
            {
                sanitized[length++] = c;
            }
            else if (c > '~') // Extended Unicode characters
            {
                // Keep extended characters but ensure we don't exceed limits
                if (length < RequestContext.MaxMetadataValueLength)
                {
                    sanitized[length++] = c;
                }
                else
                {
                    wasTruncated = true;
                }
            }
            // Control characters (0x00-0x1F, 0x7F-0x9F) are removed
        }

        var result = new string(sanitized, 0, length);

        // Check if we exceeded the limit
        if (result.Length > RequestContext.MaxMetadataValueLength)
        {
            result = result[..RequestContext.MaxMetadataValueLength];
            wasTruncated = true;
            truncated = true;
        }

        if (wasTruncated)
        {
            _logger.LogWarning("Metadata value exceeds maximum length of {MaxLength} characters and was truncated",
                RequestContext.MaxMetadataValueLength);
        }

        return result.Length == 0 ? null : result;
    }

    /// <summary>
    /// Creates and sets a new request context.
    /// </summary>
    /// <param name="requestId">The request identifier.</param>
    /// <param name="userId">Optional user identifier.</param>
    /// <param name="metadata">Optional request metadata.</param>
    /// <returns>The created request context.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="requestId"/> is null or empty.</exception>
    public RequestContext CreateContext(
        string requestId,
        string? userId = null,
        Dictionary<string, string>? metadata = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(requestId);

        // Validate and sanitize initial metadata
        var validatedMetadata = new Dictionary<string, string>(StringComparer.Ordinal);
        var totalSize = 0;
        var entryCount = 0;

        if (metadata != null)
        {
            foreach (var kvp in metadata)
            {
                if (string.IsNullOrEmpty(kvp.Key))
                    continue;

                var sanitizedKey = SanitizeMetadataKey(kvp.Key);
                if (sanitizedKey is null)
                {
                    _logger.LogWarning("Initial metadata key '{Key}' was rejected during context creation", kvp.Key);
                    continue;
                }

                var sanitizedValue = SanitizeMetadataValue(kvp.Value, out var wasTruncated);
                if (sanitizedValue is null)
                {
                    _logger.LogWarning("Initial metadata value for key '{Key}' was rejected during context creation", kvp.Key);
                    continue;
                }

                var entrySize = sanitizedKey.Length + sanitizedValue.Length;
                if (totalSize + entrySize > RequestContext.MaxTotalMetadataSize)
                {
                    _logger.LogWarning("Initial metadata entry '{Key}' exceeds total size limit and was skipped", sanitizedKey);
                    continue;
                }

                if (entryCount >= RequestContext.MaxMetadataEntries)
                {
                    _logger.LogWarning("Maximum metadata entry count reached during context creation, entry '{Key}' was skipped",
                        sanitizedKey);
                    break;
                }

                validatedMetadata[sanitizedKey] = sanitizedValue;
                totalSize += entrySize;
                entryCount++;
            }
        }

        var context = new RequestContext
        {
            RequestId = requestId,
            UserId = userId,
            StartTime = DateTime.UtcNow,
            Metadata = validatedMetadata
        };

        _context.Value = context;

        lock (_registryLock)
        {
            _activeContexts[requestId] = context;
        }

        _logger.LogDebug("Request context created: RequestId={RequestId}, UserId={UserId}, MetadataEntries={EntryCount}",
            requestId, userId ?? "anonymous", entryCount);

        return context;
    }

    /// <summary>
    /// Gets the current request context.
    /// Returns null if no context is set.
    /// </summary>
    public RequestContext? GetContext()
    {
        return _context.Value;
    }

    /// <summary>
    /// Gets the current request ID.
    /// </summary>
    public string? GetRequestId()
    {
        return _context.Value?.RequestId;
    }

    /// <summary>
    /// Gets the current user ID.
    /// </summary>
    public string? GetUserId()
    {
        return _context.Value?.UserId;
    }

    /// <summary>
    /// Sets metadata for the current request.
    /// </summary>
    /// <param name="key">The metadata key.</param>
    /// <param name="value">The metadata value.</param>
    /// <exception cref="ArgumentNullException">Thrown when key is null.</exception>
    /// <exception cref="ArgumentException">Thrown when key is empty or contains only whitespace.</exception>
    public void SetMetadata(string key, string value)
    {
        if (_context.Value is null)
        {
            _logger.LogWarning("No request context available for metadata storage");
            return;
        }

        ArgumentException.ThrowIfNullOrEmpty(key);
        ArgumentException.ThrowIfNullOrEmpty(value);

        // Sanitize the key and value
        var sanitizedKey = SanitizeMetadataKey(key);
        if (sanitizedKey is null)
        {
            _logger.LogWarning("Metadata key '{Key}' was rejected due to validation", key);
            return;
        }

        var sanitizedValue = SanitizeMetadataValue(value, out var wasTruncated);
        if (sanitizedValue is null)
        {
            _logger.LogWarning("Metadata value for key '{Key}' was rejected due to validation", key);
            return;
        }

        // Check total metadata size limit
        var currentSize = _context.Value.Metadata.Sum(kvp => kvp.Key.Length + kvp.Value.Length);
        var newEntrySize = sanitizedKey.Length + sanitizedValue.Length;

        if (currentSize + newEntrySize > RequestContext.MaxTotalMetadataSize)
        {
            _logger.LogWarning("Adding metadata entry would exceed total size limit of {MaxTotalSize} bytes",
                RequestContext.MaxTotalMetadataSize);
            return;
        }

        // Check maximum number of entries
        if (_context.Value.Metadata.Count >= RequestContext.MaxMetadataEntries)
        {
            _logger.LogWarning("Cannot add metadata entry: maximum of {MaxEntries} entries already reached",
                RequestContext.MaxMetadataEntries);
            return;
        }

        _context.Value.Metadata[sanitizedKey] = sanitizedValue;

        if (wasTruncated)
        {
            _logger.LogDebug("Metadata entry added with truncated value for key '{Key}'", sanitizedKey);
        }
        else
        {
            _logger.LogTrace("Metadata entry added: key='{Key}', value length={ValueLength}",
                sanitizedKey, sanitizedValue.Length);
        }
    }

    /// <summary>
    /// Gets metadata from the current request.
    /// </summary>
    public string? GetMetadata(string key)
    {
        ArgumentNullException.ThrowIfNull(key);
        if (_context.Value is null)
            return null;

        return _context.Value.Metadata.TryGetValue(key, out var value) ? value : null;
    }

    /// <summary>
    /// Records the elapsed time for the request.
    /// Should be called when request completes.
    /// </summary>
    public void RecordElapsedTime()
    {
        if (_context.Value is null)
            return;

        _context.Value.EndTime = DateTime.UtcNow;
        _logger.LogInformation(
            "Request completed: RequestId={RequestId}, Duration={DurationMs}ms, UserId={UserId}",
            _context.Value.RequestId,
            _context.Value.ElapsedMilliseconds,
            _context.Value.UserId ?? "anonymous");
    }

    /// <summary>
    /// Clears the current request context.
    /// Should be called when request processing is complete.
    /// </summary>
    public void Clear()
    {
        var context = _context.Value;
        _context.Value = null;

        if (context is not null)
        {
            lock (_registryLock)
            {
                _activeContexts.TryRemove(context.RequestId, out _);
            }
            _logger.LogDebug("Request context cleared: RequestId={RequestId}", context.RequestId);
        }
    }

    /// <summary>
    /// Checks if a request context is currently active.
    /// </summary>
    public bool IsContextActive()
    {
        return _context.Value is not null;
    }

    /// <summary>
    /// Gets the number of active request contexts.
    /// Useful for monitoring and diagnostics.
    /// </summary>
    /// <returns>The count of active contexts.</returns>
    public int GetActiveContextCount()
    {
        lock (_registryLock)
        {
            return _activeContexts.Count;
        }
    }

    /// <summary>
    /// Gets all active request contexts.
    /// Useful for debugging and monitoring.
    /// </summary>
    /// <returns>A collection of active request contexts.</returns>
    public IReadOnlyCollection<RequestContext> GetActiveContexts()
    {
        lock (_registryLock)
        {
            return _activeContexts.Values.ToList().AsReadOnly();
        }
    }

    /// <summary>
    /// Attempts to remove a context by request ID.
    /// Useful for cleanup of orphaned contexts.
    /// </summary>
    /// <param name="requestId">The request ID to remove.</param>
    /// <returns>True if the context was found and removed; otherwise false.</returns>
    public bool TryRemoveContext(string requestId)
    {
        ArgumentException.ThrowIfNullOrEmpty(requestId);

        lock (_registryLock)
        {
            return _activeContexts.TryRemove(requestId, out _);
        }
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 0)
        {
            lock (_registryLock)
            {
                _activeContexts.Clear();
            }
            _logger.LogInformation("RequestContextManager disposed and all active contexts cleared");
        }
    }
}

/// <summary>
/// Request context information.
/// </summary>
public sealed class RequestContext
{
    /// <summary>
    /// Maximum allowed length for any metadata key in bytes.
    /// </summary>
    public const int MaxMetadataKeyLength = 128;

    /// <summary>
    /// Maximum allowed length for any metadata value in bytes.
    /// </summary>
    public const int MaxMetadataValueLength = 4096;

    /// <summary>
    /// Maximum allowed number of metadata entries per request.
    /// </summary>
    public const int MaxMetadataEntries = 100;

    /// <summary>
    /// Maximum total size of all metadata values combined in bytes.
    /// </summary>
    public const int MaxTotalMetadataSize = 65536;

    public string RequestId { get; set; } = Guid.NewGuid().ToString();
    public string? UserId { get; set; }
    public DateTime StartTime { get; set; } = DateTime.UtcNow;
    public DateTime? EndTime { get; set; }
    public Dictionary<string, string> Metadata { get; set; } = new();

    public long ElapsedMilliseconds =>
        EndTime.HasValue ? (long)(EndTime.Value - StartTime).TotalMilliseconds : -1;

    public string? GetMetadata(string key) =>
        Metadata.TryGetValue(key, out var value) ? value : null;

    public override string ToString() =>
        $"RequestId={RequestId}, UserId={UserId ?? "anonymous"}, Duration={ElapsedMilliseconds}ms";
}

/// <summary>
/// Middleware to manage request context for all requests.
/// </summary>
public sealed class RequestContextMiddleware
{
    private readonly RequestDelegate _next;
    private readonly RequestContextManager _contextManager;
    private readonly ILogger<RequestContextMiddleware> _logger;

    public RequestContextMiddleware(
        RequestDelegate next,
        RequestContextManager contextManager,
        ILogger<RequestContextMiddleware> logger)
    {
        _next = next;
        _contextManager = contextManager;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext httpContext)
    {
        // Generate or extract request ID
        var requestId = httpContext.Request.Headers.TryGetValue("X-Request-ID", out var headerValue)
            ? headerValue.ToString()
            : Guid.NewGuid().ToString();

        // Extract user ID from claims if available
        var userId = httpContext.User?.FindFirst("sub")?.Value
            ?? httpContext.User?.Identity?.Name;

        // Create request context
        var context = _contextManager.CreateContext(requestId, userId);

        // Add request ID to response headers
        httpContext.Response.Headers.Add("X-Request-ID", requestId);

        try
        {
            await _next(httpContext).ConfigureAwait(false);
        }
        finally
        {
            _contextManager.RecordElapsedTime();
            _contextManager.Clear();
        }
    }
}

/// <summary>
/// Extension methods for request context.
/// </summary>
public static class RequestContextExtensions
{
    /// <summary>
    /// Gets the request context from HTTP context.
    /// </summary>
    public static RequestContext? GetRequestContext(this HttpContext httpContext)
    {
        return httpContext.Items.TryGetValue("RequestContext", out var context)
            ? context as RequestContext
            : null;
    }

    /// <summary>
    /// Sets the request context in HTTP context.
    /// </summary>
    public static void SetRequestContext(this HttpContext httpContext, RequestContext context)
    {
        httpContext.Items["RequestContext"] = context;
    }

    /// <summary>
    /// Registers request context middleware.
    /// </summary>
    public static IApplicationBuilder UseRequestContext(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<RequestContextMiddleware>();
    }

    /// <summary>
    /// Registers request context manager as a service.
    /// </summary>
    public static IServiceCollection AddRequestContextManager(this IServiceCollection services)
    {
        return services.AddScoped<RequestContextManager>();
    }
}
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using GrpcWebBridge.Domain;
using GrpcWebBridge.Domain.Exceptions;
using GrpcWebBridge.Domain.Models;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace GrpcWebBridge.Services;

/// <summary>
/// Service for authentication and authorization of gRPC requests
/// </summary>
public class AuthenticationService
{
    private readonly ILogger<AuthenticationService> _logger;
    private readonly ConcurrentDictionary<string, AuthenticationContext> _contextCache = new();

    public AuthenticationService(ILogger<AuthenticationService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Authenticates a bearer token
    /// </summary>
    public AuthenticationContext AuthenticateBearer(string token, string? userId = null)
    {
        if (string.IsNullOrWhiteSpace(token))
            throw new GrpcWebBridgeException("Token cannot be empty", "INVALID_TOKEN");

        try
        {
            _logger.LogInformation("Authenticating bearer token");

            var handler = new JwtSecurityTokenHandler();
            if (!handler.CanReadToken(token))
                throw new GrpcWebBridgeException("Invalid token format", "INVALID_TOKEN");

            var jwtToken = handler.ReadToken(token) as JwtSecurityToken;
            if (jwtToken is null)
                throw new GrpcWebBridgeException("Failed to parse token", "TOKEN_PARSE_ERROR");

            if (jwtToken.ValidTo < DateTime.UtcNow)
                throw new GrpcWebBridgeException("Token has expired", "TOKEN_EXPIRED");

            var context = new AuthenticationContext(
                userId ?? jwtToken.Subject ?? "unknown",
                AuthenticationScheme.Bearer,
                token);

            // Extract claims
            foreach (var claim in jwtToken.Claims)
            {
                if (claim.Type == ClaimTypes.Role)
                    context.AddRole(claim.Value);
                else
                    context.AddClaim(claim.Type, claim.Value);
            }

            if (jwtToken.ValidTo > DateTime.UtcNow)
                context.SetExpiration(jwtToken.ValidTo);

            context.Validate();
            _logger.LogInformation("Bearer authentication successful: {UserId}", context.UserId);

            CacheContext(context);
            return context;
        }
        catch (GrpcWebBridgeException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Bearer authentication failed");
            throw new GrpcWebBridgeException($"Authentication failed: {ex.Message}", "AUTH_FAILED");
        }
    }

    /// <summary>
    /// Authenticates using API key
    /// </summary>
    public AuthenticationContext AuthenticateApiKey(string apiKey, string userId)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new GrpcWebBridgeException("API key cannot be empty", "INVALID_API_KEY");

        if (string.IsNullOrWhiteSpace(userId))
            throw new GrpcWebBridgeException("User ID cannot be empty", "INVALID_USER_ID");

        _logger.LogInformation("Authenticating API key for user: {UserId}", userId);

        var context = new AuthenticationContext(userId, AuthenticationScheme.ApiKey, apiKey);
        context.SetExpiration(Constants.Authentication.JwtExpirationMinutes);

        _logger.LogInformation("API key authentication successful: {UserId}", userId);

        CacheContext(context);
        return context;
    }

    /// <summary>
    /// Authenticates using custom credentials
    /// </summary>
    public AuthenticationContext AuthenticateCustom(string userId, Dictionary<string, string> credentials)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new GrpcWebBridgeException("User ID cannot be empty", "INVALID_USER_ID");

        if (credentials is null || credentials.Count == 0)
            throw new GrpcWebBridgeException("Credentials cannot be empty", "INVALID_CREDENTIALS");

        _logger.LogInformation("Authenticating custom credentials for user: {UserId}", userId);

        var context = new AuthenticationContext(userId, AuthenticationScheme.Custom);

        foreach (var kvp in credentials)
            context.AddClaim(kvp.Key, kvp.Value);

        context.SetExpiration(Constants.Authentication.JwtExpirationMinutes);
        _logger.LogInformation("Custom authentication successful: {UserId}", userId);

        CacheContext(context);
        return context;
    }

    /// <summary>
    /// Validates an existing authentication context
    /// </summary>
    public bool ValidateContext(AuthenticationContext context)
    {
        try
        {
            if (context is null)
                return false;

            context.Validate();

            if (context.IsExpired)
            {
                _logger.LogWarning("Authentication context expired: {ContextId}", context.Id);
                return false;
            }

            return context.IsAuthenticated;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Context validation failed");
            return false;
        }
    }

    /// <summary>
    /// Checks if user has required role
    /// </summary>
    public bool AuthorizeRole(AuthenticationContext context, string requiredRole)
    {
        if (!ValidateContext(context))
            return false;

        return context.HasRole(requiredRole);
    }

    /// <summary>
    /// Checks if user has any of the required roles
    /// </summary>
    public bool AuthorizeAnyRole(AuthenticationContext context, params string[] requiredRoles)
    {
        if (!ValidateContext(context))
            return false;

        return context.HasAnyRole(requiredRoles);
    }

    /// <summary>
    /// Retrieves cached context by ID
    /// </summary>
    public AuthenticationContext? GetCachedContext(string contextId) =>
        _contextCache.TryGetValue(contextId, out var context) ? context : null;

    /// <summary>
    /// Caches an authentication context
    /// </summary>
    private void CacheContext(AuthenticationContext context) =>
        _contextCache[context.Id] = context;

    /// <summary>
    /// Creates a response for authentication failure
    /// </summary>
    public GrpcResponse CreateAuthFailureResponse(string requestId)
    {
        var response = new GrpcResponse { RequestId = requestId };
        response.SetError(GrpcStatusCode.Unauthenticated, "Authentication required");

        _logger.LogWarning("Created auth failure response for request: {RequestId}", requestId);

        return response;
    }

    /// <summary>
    /// Extracts bearer token from authorization header
    /// </summary>
    public string? ExtractBearerToken(string? authHeader)
    {
        if (string.IsNullOrWhiteSpace(authHeader))
            return null;

        const string bearerPrefix = "Bearer ";
        var span = authHeader.AsSpan();
        if (!span.StartsWith(bearerPrefix, StringComparison.OrdinalIgnoreCase))
            return null;

        var token = span[bearerPrefix.Length..].Trim();
        return token.Length > 0 ? token.ToString() : null;
    }
}

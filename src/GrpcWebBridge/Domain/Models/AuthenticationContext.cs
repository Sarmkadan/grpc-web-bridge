#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Security.Claims;

namespace GrpcWebBridge.Domain.Models;

/// <summary>
/// Represents authentication context for a request or stream
/// </summary>
public sealed class AuthenticationContext
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public AuthenticationScheme Scheme { get; set; } = AuthenticationScheme.None;
    public string? Token { get; set; }
    public string? UserId { get; set; }
    public string? Username { get; set; }
    public List<string> Roles { get; set; } = [];
    public Dictionary<string, string> Claims { get; set; } = [];
    public DateTime? ExpiresAt { get; set; }
    public DateTime AuthenticatedAt { get; set; } = DateTime.UtcNow;
    public bool IsAuthenticated { get; set; }
    public bool IsExpired => ExpiresAt.HasValue && ExpiresAt.Value < DateTime.UtcNow;
    public string? IpAddress { get; set; }
    public Dictionary<string, object> CustomData { get; set; } = [];

    public AuthenticationContext() { }

    public AuthenticationContext(string userId, AuthenticationScheme scheme, string? token = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(userId);
        UserId = ValidateUserId(userId);
        Scheme = scheme;
        Token = token;
        IsAuthenticated = true;
    }

    public void AddRole(string role)
    {
        ArgumentException.ThrowIfNullOrEmpty(role);
        if (string.IsNullOrWhiteSpace(role))
            throw new ArgumentException("Role cannot be empty", nameof(role));

        if (!Roles.Contains(role))
            Roles.Add(role);
    }

    public bool HasRole(string role)
    {
        ArgumentException.ThrowIfNullOrEmpty(role);
        if (string.IsNullOrWhiteSpace(role))
            return false;

        return Roles.Contains(role);
    }

    public bool HasAnyRole(params string[] roles)
    {
        ArgumentNullException.ThrowIfNull(roles);
        if (roles.Length == 0)
            return false;

        return roles.Any(HasRole);
    }

    public bool HasAllRoles(params string[] roles)
    {
        ArgumentNullException.ThrowIfNull(roles);
        if (roles.Length == 0)
            return false;

        return roles.All(HasRole);
    }

    public void AddClaim(string key, string value)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);
        ArgumentException.ThrowIfNullOrEmpty(value);
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Claim key cannot be empty", nameof(key));

        Claims[key] = value;
    }

    public string? GetClaim(string key)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);
        return Claims.TryGetValue(key, out var value) ? value : null;
    }

    public bool HasClaim(string key)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);
        return Claims.ContainsKey(key);
    }

    public void SetExpiration(int minutesFromNow)
    {
        if (minutesFromNow <= 0)
            throw new ArgumentException("Minutes must be greater than 0", nameof(minutesFromNow));

        ExpiresAt = DateTime.UtcNow.AddMinutes(minutesFromNow);
    }

    public void SetExpiration(DateTime expirationTime)
    {
        if (expirationTime <= DateTime.UtcNow)
            throw new ArgumentException("Expiration time must be in the future", nameof(expirationTime));

        ExpiresAt = expirationTime;
    }

    public TimeSpan GetRemainingTime()
    {
        if (IsExpired)
            return TimeSpan.Zero;

        if (!ExpiresAt.HasValue)
            return TimeSpan.MaxValue;

        return ExpiresAt.Value - DateTime.UtcNow;
    }

    public void AddCustomData(string key, object value)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);
        ArgumentNullException.ThrowIfNull(value);
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Key cannot be empty", nameof(key));

        CustomData[key] = value;
    }

    public object? GetCustomData(string key)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);
        return CustomData.TryGetValue(key, out var value) ? value : null;
    }

    public void Validate()
    {
        if (!IsAuthenticated)
        {
            if (string.IsNullOrWhiteSpace(UserId))
                throw new ArgumentException("User ID is required for authenticated context", nameof(UserId));

            if (string.IsNullOrWhiteSpace(Token) && Scheme != AuthenticationScheme.None)
                throw new ArgumentException("Token is required for bearer authentication", nameof(Token));
        }

        if (IsExpired)
            throw new InvalidOperationException("Authentication context has expired");
    }

    private static string ValidateUserId(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("User ID cannot be empty", nameof(userId));
        return userId.Trim();
    }

    public override string ToString() => $"AuthContext {Id}: {Scheme} {UserId ?? "anonymous"}";

    public override bool Equals(object? obj)
    {
        if (obj is not AuthenticationContext other)
            return false;

        return Id == other.Id;
    }

    public override int GetHashCode() => Id.GetHashCode();
}
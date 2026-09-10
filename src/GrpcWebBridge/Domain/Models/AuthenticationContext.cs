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
    /// <summary>
    /// Gets or sets the unique identifier for this authentication context.
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>
    /// Gets or sets the authentication scheme used.
    /// </summary>
    public AuthenticationScheme Scheme { get; set; } = AuthenticationScheme.None;

    /// <summary>
    /// Gets or sets the authentication token.
    /// </summary>
    public string? Token { get; set; }

    /// <summary>
    /// Gets or sets the user identifier.
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Gets or sets the user name.
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// Gets or sets the list of roles assigned to the user.
    /// </summary>
    public List<string> Roles { get; set; } = [];

    /// <summary>
    /// Gets or sets the collection of claims.
    /// </summary>
    public Dictionary<string, string> Claims { get; set; } = [];

    /// <summary>
    /// Gets or sets the expiration date and time of the authentication context.
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the authentication occurred.
    /// </summary>
    public DateTime AuthenticatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets a value indicating whether the user is authenticated.
    /// </summary>
    public bool IsAuthenticated { get; set; }

    /// <summary>
    /// Gets a value indicating whether the authentication context has expired.
    /// </summary>
    public bool IsExpired => ExpiresAt.HasValue && ExpiresAt.Value < DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the IP address of the client.
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// Gets or sets the custom data associated with the authentication context.
    /// </summary>
    public Dictionary<string, object> CustomData { get; set; } = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthenticationContext"/> class.
    /// </summary>
    public AuthenticationContext() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthenticationContext"/> class with the specified user ID, authentication scheme, and optional token.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="scheme">The authentication scheme.</param>
    /// <param name="token">The authentication token (optional).</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="userId"/> is null, empty, or consists only of white-space characters.</exception>
    public AuthenticationContext(string userId, AuthenticationScheme scheme, string? token = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(userId);
        UserId = ValidateUserId(userId);
        Scheme = scheme;
        Token = token;
        IsAuthenticated = true;
    }

    /// <summary>
    /// Adds a role to the authentication context.
    /// </summary>
    /// <param name="role">The role to add.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="role"/> is null, empty, or consists only of white-space characters.</exception>
    public void AddRole(string role)
    {
        ArgumentException.ThrowIfNullOrEmpty(role);
        if (string.IsNullOrWhiteSpace(role))
            throw new ArgumentException("Role cannot be empty", nameof(role));

        if (!Roles.Contains(role))
            Roles.Add(role);
    }

    /// <summary>
    /// Determines whether the authentication context has the specified role.
    /// </summary>
    /// <param name="role">The role to check.</param>
    /// <returns>true if the role exists; otherwise, false.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="role"/> is null, empty, or consists only of white-space characters.</exception>
    public bool HasRole(string role)
    {
        ArgumentException.ThrowIfNullOrEmpty(role);
        if (string.IsNullOrWhiteSpace(role))
            return false;

        return Roles.Contains(role);
    }

    /// <summary>
    /// Determines whether the authentication context has any of the specified roles.
    /// </summary>
    /// <param name="roles">The roles to check.</param>
    /// <returns>true if at least one role exists; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="roles"/> is null.</exception>
    public bool HasAnyRole(params string[] roles)
    {
        ArgumentNullException.ThrowIfNull(roles);
        if (roles.Length == 0)
            return false;

        return roles.Any(HasRole);
    }

    /// <summary>
    /// Determines whether the authentication context has all of the specified roles.
    /// </summary>
    /// <param name="roles">The roles to check.</param>
    /// <returns>true if all roles exist; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="roles"/> is null.</exception>
    public bool HasAllRoles(params string[] roles)
    {
        ArgumentNullException.ThrowIfNull(roles);
        if (roles.Length == 0)
            return false;

        return roles.All(HasRole);
    }

    /// <summary>
    /// Adds a claim to the authentication context.
    /// </summary>
    /// <param name="key">The claim key.</param>
    /// <param name="value">The claim value.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="key"/> or <paramref name="value"/> is null, empty, or consists only of white-space characters.</exception>
    public void AddClaim(string key, string value)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);
        ArgumentException.ThrowIfNullOrEmpty(value);
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Claim key cannot be empty", nameof(key));

        Claims[key] = value;
    }

    /// <summary>
    /// Gets the claim value associated with the specified key.
    /// </summary>
    /// <param name="key">The claim key.</param>
    /// <returns>The claim value if found; otherwise, null.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="key"/> is null, empty, or consists only of white-space characters.</exception>
    public string? GetClaim(string key)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);
        return Claims.TryGetValue(key, out var value) ? value : null;
    }

    /// <summary>
    /// Determines whether the authentication context contains the specified claim key.
    /// </summary>
    /// <param name="key">The claim key to check.</param>
    /// <returns>true if the claim key exists; otherwise, false.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="key"/> is null, empty, or consists only of white-space characters.</exception>
    public bool HasClaim(string key)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);
        return Claims.ContainsKey(key);
    }

    /// <summary>
    /// Sets the expiration time based on the number of minutes from now.
    /// </summary>
    /// <param name="minutesFromNow">The number of minutes from now to set the expiration time.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="minutesFromNow"/> is less than or equal to zero.</exception>
    public void SetExpiration(int minutesFromNow)
    {
        if (minutesFromNow <= 0)
            throw new ArgumentException("Minutes must be greater than 0", nameof(minutesFromNow));

        ExpiresAt = DateTime.UtcNow.AddMinutes(minutesFromNow);
    }

    /// <summary>
    /// Sets the expiration time to the specified date and time.
    /// </summary>
    /// <param name="expirationTime">The expiration time.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="expirationTime"/> is earlier than or equal to the current UTC date and time.</exception>
    public void SetExpiration(DateTime expirationTime)
    {
        if (expirationTime <= DateTime.UtcNow)
            throw new ArgumentException("Expiration time must be in the future", nameof(expirationTime));

        ExpiresAt = expirationTime;
    }

    /// <summary>
    /// Gets the remaining time until the authentication context expires.
    /// </summary>
    /// <returns>
    /// A <see cref="TimeSpan"/> representing the remaining time. Returns <see cref="TimeSpan.Zero"/> if expired;
    /// <see cref="TimeSpan.MaxValue"/> if no expiration is set.
    /// </returns>
    public TimeSpan GetRemainingTime()
    {
        if (IsExpired)
            return TimeSpan.Zero;

        if (!ExpiresAt.HasValue)
            return TimeSpan.MaxValue;

        return ExpiresAt.Value - DateTime.UtcNow;
    }

    /// <summary>
    /// Adds custom data to the authentication context.
    /// </summary>
    /// <param name="key">The key of the custom data.</param>
    /// <param name="value">The value of the custom data.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="key"/> is null, empty, or consists only of white-space characters.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    public void AddCustomData(string key, object value)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);
        ArgumentNullException.ThrowIfNull(value);
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Key cannot be empty", nameof(key));

        CustomData[key] = value;
    }

    /// <summary>
    /// Gets the custom data value associated with the specified key.
    /// </summary>
    /// <param name="key">The key of the custom data to retrieve.</param>
    /// <returns>The custom data value if found; otherwise, null.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="key"/> is null, empty, or consists only of white-space characters.</exception>
    public object? GetCustomData(string key)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);
        return CustomData.TryGetValue(key, out var value) ? value : null;
    }

    /// <summary>
    /// Validates the authentication context.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when the user ID is required but missing, or when a token is required for bearer authentication but missing.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the authentication context has expired.</exception>
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

    /// <summary>
    /// Returns a string representation of the authentication context.
    /// </summary>
    /// <returns>A string in the format "AuthContext {Id}: {Scheme} {UserId or 'anonymous'}".</returns>
    public override string ToString() => $"AuthContext {Id}: {Scheme} {UserId ?? "anonymous"}";

    /// <summary>
    /// Determines whether the specified object is equal to the current authentication context.
    /// </summary>
    /// <param name="obj">The object to compare with the current authentication context.</param>
    /// <returns>true if the specified object is an authentication context with the same ID; otherwise, false.</returns>
    public override bool Equals(object? obj)
    {
        if (obj is not AuthenticationContext other)
            return false;

        return Id == other.Id;
    }

    /// <summary>
    /// Serves as the default hash function.
    /// </summary>
    /// <returns>A hash code for the current authentication context.</returns>
    public override int GetHashCode() => Id.GetHashCode();
}
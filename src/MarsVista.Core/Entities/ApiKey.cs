namespace MarsVista.Core.Entities;

/// <summary>
/// Represents a user's API key for authenticating requests to the Mars Vista API.
/// Linked to Auth.js User table by email (stored in separate auth database).
/// </summary>
public class ApiKey : ITimestamped
{
    /// <summary>
    /// Unique identifier for the API key record
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// User's email address (links to Auth.js User.email in separate auth database).
    /// One user can have one API key (enforced by unique constraint).
    /// </summary>
    public string UserEmail { get; set; } = string.Empty;

    /// <summary>
    /// SHA-256 hash of the API key. Never store plaintext keys.
    /// </summary>
    public string ApiKeyHash { get; set; } = string.Empty;

    /// <summary>
    /// User's subscription tier: 'free', 'pro', or 'enterprise'
    /// Determines rate limits and features available.
    /// </summary>
    public string Tier { get; set; } = "free";

    /// <summary>
    /// User's role: 'user' (default) or 'admin'
    /// Admin role grants access to admin dashboard and endpoints.
    /// </summary>
    public string Role { get; set; } = "user";

    /// <summary>
    /// Whether this API key is active and can be used.
    /// Set to false when regenerating to invalidate old keys.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// When the API key was last used to make a request.
    /// Updated on each successful authentication.
    /// </summary>
    public DateTime? LastUsedAt { get; set; }

    // Timestamps (from ITimestamped)
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

namespace MarsVista.Api.Services;

/// <summary>
/// Service for generating, hashing, and validating API keys.
/// Pure calculation layer - no side effects (database writes happen in controllers).
/// </summary>
public interface IApiKeyService
{
    /// <summary>
    /// Generates a new API key in format: mv_live_{40_hex_chars}
    /// Example: mv_live_a1b2c3d4e5f6789012345678901234567890abcd
    /// </summary>
    /// <returns>Plaintext API key (47 characters total)</returns>
    string GenerateApiKey();

    /// <summary>
    /// Computes SHA-256 hash of an API key for secure storage.
    /// Never store plaintext API keys in the database.
    /// </summary>
    /// <param name="apiKey">The plaintext API key to hash</param>
    /// <returns>SHA-256 hash as 64-character hex string</returns>
    string HashApiKey(string apiKey);

    /// <summary>
    /// Validates that an API key matches the expected format.
    /// Format: mv_live_{40_hex_chars}
    /// </summary>
    /// <param name="apiKey">The API key to validate</param>
    /// <returns>True if format is valid, false otherwise</returns>
    bool ValidateApiKeyFormat(string apiKey);

    /// <summary>
    /// Masks an API key for display purposes.
    /// Example: mv_live_a1b2c3...7890abcd (shows first 10 and last 8 chars)
    /// </summary>
    /// <param name="apiKey">The plaintext API key to mask</param>
    /// <returns>Masked API key safe for display</returns>
    string MaskApiKey(string apiKey);
}

using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace MarsVista.Api.Services;

/// <summary>
/// Implementation of API key generation, hashing, and validation.
/// Pure functions with no side effects (calculation layer).
/// </summary>
public class ApiKeyService : IApiKeyService
{
    private const string Prefix = "mv";
    private const string Environment = "live";
    private const int RandomLength = 40; // 40 hex characters = 160 bits entropy
    private const int TotalLength = 47; // "mv_live_" (8) + 40 hex chars (40) = 48, but "mv" (2) + "_" (1) + "live" (4) + "_" (1) = 8, so 8 + 40 - 1 = 47

    private static readonly Regex ApiKeyFormatRegex = new(
        @"^mv_live_[a-f0-9]{40}$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase
    );

    /// <summary>
    /// Generates a cryptographically secure random API key.
    /// Format: mv_live_{40_hex_chars}
    /// </summary>
    public string GenerateApiKey()
    {
        // Generate 20 random bytes (40 hex characters when converted)
        var randomBytes = new byte[20];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomBytes);
        }

        // Convert to hex string (40 characters)
        var randomHex = Convert.ToHexString(randomBytes).ToLowerInvariant();

        // Construct API key: mv_live_{random}
        return $"{Prefix}_{Environment}_{randomHex}";
    }

    /// <summary>
    /// Computes SHA-256 hash of API key for secure storage.
    /// </summary>
    public string HashApiKey(string apiKey)
    {
        if (string.IsNullOrEmpty(apiKey))
        {
            throw new ArgumentException("API key cannot be null or empty", nameof(apiKey));
        }

        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(apiKey);
        var hashBytes = sha256.ComputeHash(bytes);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    /// <summary>
    /// Validates API key format using regex.
    /// </summary>
    public bool ValidateApiKeyFormat(string apiKey)
    {
        if (string.IsNullOrEmpty(apiKey))
        {
            return false;
        }

        return ApiKeyFormatRegex.IsMatch(apiKey);
    }

    /// <summary>
    /// Masks API key for safe display.
    /// Shows: mv_live_abc...xyz (first 10 chars + ... + last 8 chars)
    /// </summary>
    public string MaskApiKey(string apiKey)
    {
        if (string.IsNullOrEmpty(apiKey) || apiKey.Length < 18)
        {
            return "****";
        }

        // Show mv_live_abc (first 10 chars) + ... + last 8 chars
        var prefix = apiKey[..10]; // "mv_live_ab"
        var suffix = apiKey[^8..]; // last 8 chars
        return $"{prefix}...{suffix}";
    }
}

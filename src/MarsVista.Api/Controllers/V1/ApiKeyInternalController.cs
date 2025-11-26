using MarsVista.Core.Data;
using MarsVista.Core.Entities;
using MarsVista.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MarsVista.Api.Controllers.V1;

/// <summary>
/// Internal API for API key management.
/// Called by Next.js frontend after validating Auth.js sessions.
/// Protected by InternalApiMiddleware (X-Internal-Secret header).
/// </summary>
[ApiController]
[Route("api/v1/internal/keys")]
[ApiExplorerSettings(IgnoreApi = true)] // Exclude from public API documentation
public class ApiKeyInternalController : ControllerBase
{
    private readonly MarsVistaDbContext _context;
    private readonly IApiKeyService _apiKeyService;
    private readonly ILogger<ApiKeyInternalController> _logger;

    public ApiKeyInternalController(
        MarsVistaDbContext context,
        IApiKeyService apiKeyService,
        ILogger<ApiKeyInternalController> logger)
    {
        _context = context;
        _apiKeyService = apiKeyService;
        _logger = logger;
    }

    /// <summary>
    /// Generate a new API key for a user.
    /// Called by Next.js after validating Auth.js session.
    /// </summary>
    /// <param name="request">Request containing user email</param>
    /// <returns>The generated API key (plaintext - only time it's visible)</returns>
    [HttpPost("generate")]
    public async Task<IActionResult> GenerateApiKey([FromBody] GenerateApiKeyRequest request)
    {
        if (string.IsNullOrEmpty(request.UserEmail))
        {
            return BadRequest(new { error = "user_email is required" });
        }

        // Validate email format
        if (!IsValidEmail(request.UserEmail))
        {
            return BadRequest(new { error = "Invalid email format" });
        }

        _logger.LogInformation("Generating API key for user {Email}", request.UserEmail);

        // Check if user already has an API key
        var existingKey = await _context.ApiKeys
            .Where(k => k.UserEmail == request.UserEmail)
            .FirstOrDefaultAsync();

        if (existingKey != null)
        {
            _logger.LogWarning("User {Email} already has an API key", request.UserEmail);
            return Conflict(new
            {
                error = "User already has an API key",
                message = "Please regenerate your existing key instead of creating a new one"
            });
        }

        // Generate new API key
        var apiKey = _apiKeyService.GenerateApiKey();
        var apiKeyHash = _apiKeyService.HashApiKey(apiKey);

        // Create database record
        var apiKeyRecord = new ApiKey
        {
            Id = Guid.NewGuid(),
            UserEmail = request.UserEmail,
            ApiKeyHash = apiKeyHash,
            Tier = "pro", // All users get pro tier (generous rate limits)
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.ApiKeys.Add(apiKeyRecord);
        await _context.SaveChangesAsync();

        _logger.LogInformation("API key created for user {Email} with ID {Id}", request.UserEmail, apiKeyRecord.Id);

        return Ok(new
        {
            apiKey, // Plaintext key - only time it's returned
            tier = apiKeyRecord.Tier,
            createdAt = apiKeyRecord.CreatedAt
        });
    }

    /// <summary>
    /// Regenerate an existing API key (invalidates the old one).
    /// Called by Next.js after validating Auth.js session.
    /// </summary>
    /// <param name="request">Request containing user email</param>
    /// <returns>The new API key (plaintext)</returns>
    [HttpPost("regenerate")]
    public async Task<IActionResult> RegenerateApiKey([FromBody] RegenerateApiKeyRequest request)
    {
        if (string.IsNullOrEmpty(request.UserEmail))
        {
            return BadRequest(new { error = "user_email is required" });
        }

        _logger.LogInformation("Regenerating API key for user {Email}", request.UserEmail);

        // Find existing API key
        var existingKey = await _context.ApiKeys
            .Where(k => k.UserEmail == request.UserEmail)
            .FirstOrDefaultAsync();

        if (existingKey == null)
        {
            _logger.LogWarning("No API key found for user {Email}", request.UserEmail);
            return NotFound(new
            {
                error = "No API key found",
                message = "Please generate an API key first"
            });
        }

        // Generate new API key
        var newApiKey = _apiKeyService.GenerateApiKey();
        var newApiKeyHash = _apiKeyService.HashApiKey(newApiKey);

        // Update existing record
        existingKey.ApiKeyHash = newApiKeyHash;
        existingKey.UpdatedAt = DateTime.UtcNow;
        existingKey.IsActive = true; // Reactivate if it was deactivated
        existingKey.LastUsedAt = null; // Reset last used

        await _context.SaveChangesAsync();

        _logger.LogInformation("API key regenerated for user {Email}", request.UserEmail);

        return Ok(new
        {
            apiKey = newApiKey, // Plaintext key - only time it's returned
            tier = existingKey.Tier,
            createdAt = existingKey.CreatedAt
        });
    }

    /// <summary>
    /// Get current API key information (masked).
    /// Called by Next.js to display key info in dashboard.
    /// </summary>
    /// <param name="userEmail">User's email address</param>
    /// <returns>Masked API key and metadata</returns>
    [HttpGet("current")]
    public async Task<IActionResult> GetCurrentApiKey([FromQuery] string userEmail)
    {
        if (string.IsNullOrEmpty(userEmail))
        {
            return BadRequest(new { error = "userEmail query parameter is required" });
        }

        var apiKeyRecord = await _context.ApiKeys
            .Where(k => k.UserEmail == userEmail)
            .FirstOrDefaultAsync();

        if (apiKeyRecord == null)
        {
            return NotFound(new
            {
                error = "No API key found",
                message = "User has not generated an API key yet"
            });
        }

        // Return masked key (never return plaintext from this endpoint)
        return Ok(new
        {
            keyPreview = "mv_live_***...***", // Generic preview (can't unmask from hash)
            tier = apiKeyRecord.Tier,
            isActive = apiKeyRecord.IsActive,
            createdAt = apiKeyRecord.CreatedAt,
            lastUsedAt = apiKeyRecord.LastUsedAt
        });
    }

    /// <summary>
    /// Delete a user's account and API key.
    /// Called by Next.js after validating Auth.js session.
    /// This only deletes the API key from the photos database.
    /// The Auth.js user record is deleted separately by Next.js.
    /// </summary>
    /// <param name="request">Request containing user email</param>
    /// <returns>Success confirmation</returns>
    [HttpDelete("delete")]
    public async Task<IActionResult> DeleteAccount([FromBody] DeleteAccountRequest request)
    {
        if (string.IsNullOrEmpty(request.UserEmail))
        {
            return BadRequest(new { error = "user_email is required" });
        }

        _logger.LogInformation("Deleting account for user {Email}", request.UserEmail);

        // Find and delete API key
        var apiKey = await _context.ApiKeys
            .Where(k => k.UserEmail == request.UserEmail)
            .FirstOrDefaultAsync();

        if (apiKey != null)
        {
            _context.ApiKeys.Remove(apiKey);
            await _context.SaveChangesAsync();
            _logger.LogInformation("API key deleted for user {Email}", request.UserEmail);
        }
        else
        {
            _logger.LogInformation("No API key found for user {Email}, nothing to delete", request.UserEmail);
        }

        return Ok(new
        {
            success = true,
            message = "Account data deleted successfully"
        });
    }

    /// <summary>
    /// Simple email validation
    /// </summary>
    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }
}

/// <summary>
/// Request model for generating a new API key
/// </summary>
public record GenerateApiKeyRequest(string UserEmail);

/// <summary>
/// Request model for regenerating an existing API key
/// </summary>
public record RegenerateApiKeyRequest(string UserEmail);

/// <summary>
/// Request model for deleting a user account
/// </summary>
public record DeleteAccountRequest(string UserEmail);

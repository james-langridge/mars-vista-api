namespace MarsVista.Api.Middleware;

/// <summary>
/// Middleware to protect internal API endpoints that should only be called by trusted services (Next.js frontend).
/// Validates X-Internal-Secret header against configured secret.
/// Only applies to /api/v1/internal/* paths.
/// </summary>
public class InternalApiMiddleware
{
    private readonly RequestDelegate _next;
    private readonly string? _internalSecret;
    private readonly ILogger<InternalApiMiddleware> _logger;

    public InternalApiMiddleware(
        RequestDelegate next,
        IConfiguration configuration,
        ILogger<InternalApiMiddleware> logger)
    {
        _next = next;
        _internalSecret = configuration["INTERNAL_API_SECRET"]
                       ?? Environment.GetEnvironmentVariable("INTERNAL_API_SECRET");
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Only apply to internal API endpoints
        if (!context.Request.Path.StartsWithSegments("/api/v1/internal"))
        {
            await _next(context);
            return;
        }

        // Check if internal secret is configured
        if (string.IsNullOrEmpty(_internalSecret))
        {
            _logger.LogError("INTERNAL_API_SECRET not configured - internal API is unprotected!");
            context.Response.StatusCode = 500;
            await context.Response.WriteAsJsonAsync(new
            {
                error = "Internal Server Error",
                message = "Internal API authentication not configured"
            });
            return;
        }

        // Extract X-Internal-Secret header
        var providedSecret = context.Request.Headers["X-Internal-Secret"].FirstOrDefault();

        if (string.IsNullOrEmpty(providedSecret))
        {
            _logger.LogWarning(
                "Internal API request without secret from {IP} to {Path}",
                context.Connection.RemoteIpAddress,
                context.Request.Path);

            context.Response.StatusCode = 403;
            await context.Response.WriteAsJsonAsync(new
            {
                error = "Forbidden",
                message = "Internal API requires X-Internal-Secret header"
            });
            return;
        }

        // Validate secret (constant-time comparison to prevent timing attacks)
        if (!CryptographicEquals(providedSecret, _internalSecret))
        {
            _logger.LogWarning(
                "Invalid internal API secret attempt from {IP} to {Path}",
                context.Connection.RemoteIpAddress,
                context.Request.Path);

            context.Response.StatusCode = 403;
            await context.Response.WriteAsJsonAsync(new
            {
                error = "Forbidden",
                message = "Invalid internal API secret"
            });
            return;
        }

        // Valid secret - continue to next middleware
        await _next(context);
    }

    /// <summary>
    /// Constant-time string comparison to prevent timing attacks.
    /// </summary>
    private static bool CryptographicEquals(string a, string b)
    {
        if (a.Length != b.Length)
        {
            return false;
        }

        var result = 0;
        for (var i = 0; i < a.Length; i++)
        {
            result |= a[i] ^ b[i];
        }

        return result == 0;
    }
}

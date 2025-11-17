namespace MarsVista.Api.Middleware;

/// <summary>
/// Simple API key authentication middleware for protecting the API during development.
/// Requires X-API-Key header or api_key query parameter to match hardcoded key.
/// </summary>
public class ApiKeyMiddleware
{
    private readonly RequestDelegate _next;
    private readonly string? _apiKey;
    private readonly ILogger<ApiKeyMiddleware> _logger;

    public ApiKeyMiddleware(RequestDelegate next, IConfiguration configuration, ILogger<ApiKeyMiddleware> logger)
    {
        _next = next;
        _apiKey = configuration["API_KEY"] ?? Environment.GetEnvironmentVariable("API_KEY");
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip authentication if no API key is configured (disabled)
        if (string.IsNullOrEmpty(_apiKey))
        {
            _logger.LogWarning("API_KEY not configured - API is unprotected!");
            await _next(context);
            return;
        }

        // Skip authentication for health check endpoint
        if (context.Request.Path.StartsWithSegments("/health"))
        {
            await _next(context);
            return;
        }

        // Check for API key in header (X-API-Key) or query parameter (api_key)
        var providedKey = context.Request.Headers["X-API-Key"].FirstOrDefault()
                       ?? context.Request.Query["api_key"].FirstOrDefault();

        if (string.IsNullOrEmpty(providedKey))
        {
            _logger.LogWarning("API request without API key from {IP}", context.Connection.RemoteIpAddress);
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new
            {
                error = "Unauthorized",
                message = "API key required. Provide via X-API-Key header or api_key query parameter."
            });
            return;
        }

        if (providedKey != _apiKey)
        {
            _logger.LogWarning("Invalid API key attempt from {IP}", context.Connection.RemoteIpAddress);
            context.Response.StatusCode = 403;
            await context.Response.WriteAsJsonAsync(new
            {
                error = "Forbidden",
                message = "Invalid API key."
            });
            return;
        }

        // Valid API key - continue to next middleware
        await _next(context);
    }
}

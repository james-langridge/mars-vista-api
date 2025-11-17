using System.Text;
using System.Text.Json;

namespace MarsVista.Api.Middleware;

/// <summary>
/// Middleware that supports dynamic JSON naming policy based on ?format query parameter.
/// Default (no parameter): snake_case (via JsonPropertyName attributes on DTOs)
/// ?format=camelCase: Converts response to camelCase property names
/// </summary>
public class JsonFormatMiddleware
{
    private readonly RequestDelegate _next;

    public JsonFormatMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Check if format=camelCase is requested
        var formatParam = context.Request.Query["format"].FirstOrDefault();
        var useCamelCase = formatParam?.Equals("camelCase", StringComparison.OrdinalIgnoreCase) == true;

        if (!useCamelCase)
        {
            // No format conversion needed, pass through
            await _next(context);
            return;
        }

        // Capture the original response body stream
        var originalBodyStream = context.Response.Body;

        using var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        // Execute the rest of the pipeline
        await _next(context);

        // Only process JSON responses
        if (context.Response.ContentType?.Contains("application/json") == true && responseBody.Length > 0)
        {
            responseBody.Seek(0, SeekOrigin.Begin);
            var responseText = await new StreamReader(responseBody).ReadToEndAsync();

            try
            {
                // Parse the snake_case JSON
                using var jsonDocument = JsonDocument.Parse(responseText);

                // Convert snake_case keys to camelCase
                var convertedObject = ConvertToCamelCase(jsonDocument.RootElement);

                // Serialize with standard JSON options
                var camelCaseJson = JsonSerializer.Serialize(convertedObject, new JsonSerializerOptions
                {
                    WriteIndented = false
                });

                // Write the converted response
                context.Response.Body = originalBodyStream;
                context.Response.ContentLength = Encoding.UTF8.GetByteCount(camelCaseJson);
                await context.Response.WriteAsync(camelCaseJson);
            }
            catch
            {
                // If conversion fails, return original response
                responseBody.Seek(0, SeekOrigin.Begin);
                context.Response.Body = originalBodyStream;
                await responseBody.CopyToAsync(originalBodyStream);
            }
        }
        else
        {
            // Non-JSON response, pass through unchanged
            responseBody.Seek(0, SeekOrigin.Begin);
            context.Response.Body = originalBodyStream;
            await responseBody.CopyToAsync(originalBodyStream);
        }
    }

    private static object? ConvertToCamelCase(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Object => element.EnumerateObject()
                .ToDictionary(
                    prop => ToCamelCase(prop.Name),
                    prop => ConvertToCamelCase(prop.Value)
                ),
            JsonValueKind.Array => element.EnumerateArray()
                .Select(ConvertToCamelCase)
                .ToList(),
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.TryGetInt32(out var intVal) ? intVal : element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            _ => null
        };
    }

    private static string ToCamelCase(string snakeCase)
    {
        if (string.IsNullOrEmpty(snakeCase))
            return snakeCase;

        // Convert snake_case to camelCase
        var parts = snakeCase.Split('_');
        if (parts.Length == 1)
            return snakeCase; // No underscores, return as-is

        var result = parts[0].ToLower();
        for (int i = 1; i < parts.Length; i++)
        {
            if (parts[i].Length > 0)
            {
                result += char.ToUpper(parts[i][0]) + parts[i].Substring(1).ToLower();
            }
        }
        return result;
    }
}

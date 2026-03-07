using Microsoft.AspNetCore.Antiforgery;

namespace DistroCv.Api.Middleware;

/// <summary>
/// Middleware for CSRF (Cross-Site Request Forgery) protection
/// Validates anti-forgery tokens for state-changing operations
/// </summary>
public class CsrfProtectionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IAntiforgery _antiforgery;
    private readonly ILogger<CsrfProtectionMiddleware> _logger;

    // HTTP methods that require CSRF protection
    private static readonly HashSet<string> ProtectedMethods = new(StringComparer.OrdinalIgnoreCase)
    {
        "POST", "PUT", "DELETE", "PATCH"
    };

    // Path prefixes that are exempt from CSRF protection.
    // All /api/ paths are exempt because this is a REST API using JWT Bearer tokens:
    // - Unauthenticated endpoints (login, register, forgot-password, etc.) have no browser-attachable credentials, so CSRF is not a risk.
    // - Authenticated endpoints use JWT in the Authorization header (set by JS), which browsers cannot forge cross-site.
    private static readonly string[] ExemptPrefixes =
    [
        "/api/",
        "/health",
        "/hangfire",
        "/hubs/",
        "/scalar"
    ];

    public CsrfProtectionMiddleware(
        RequestDelegate next,
        IAntiforgery antiforgery,
        ILogger<CsrfProtectionMiddleware> logger)
    {
        _next = next;
        _antiforgery = antiforgery;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? string.Empty;
        var method = context.Request.Method;

        // ALL /api/ endpoints use JWT Bearer tokens, not cookies.
        // CSRF only threatens cookie-based auth. Skip everything under /api/.
        if (!ProtectedMethods.Contains(method))
        {
            await _next(context);
            return;
        }

        // Exempt all API and infrastructure paths
        foreach (var prefix in ExemptPrefixes)
        {
            if (path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                await _next(context);
                return;
            }
        }

        // JWT Bearer header present — skip CSRF
        if (IsJwtAuthenticated(context))
        {
            await _next(context);
            return;
        }

        // Non-API routes (MVC/Razor if any) — validate CSRF
        try
        {
            await _antiforgery.ValidateRequestAsync(context);
        }
        catch (AntiforgeryValidationException ex)
        {
            _logger.LogWarning(ex, "CSRF validation failed for {Path}", path);
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new
            {
                error = "CSRF validation failed",
                message = "Invalid or missing anti-forgery token"
            });
            return;
        }

        await _next(context);
    }

    /// <summary>
    /// Check if request is authenticated with JWT Bearer token
    /// JWT-authenticated requests don't need CSRF protection as they can't be forged by browsers
    /// </summary>
    private static bool IsJwtAuthenticated(HttpContext context)
    {
        var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
        return !string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase);
    }
}

/// <summary>
/// Extension method to add CSRF protection middleware
/// </summary>
public static class CsrfProtectionMiddlewareExtensions
{
    public static IApplicationBuilder UseCsrfProtection(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<CsrfProtectionMiddleware>();
    }
}

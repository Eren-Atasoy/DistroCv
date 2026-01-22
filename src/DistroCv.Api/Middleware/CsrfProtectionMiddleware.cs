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

    // Paths that are exempt from CSRF protection (e.g., API endpoints using JWT)
    private static readonly HashSet<string> ExemptPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "/api/auth/signin",
        "/api/auth/signup",
        "/api/auth/refresh",
        "/api/auth/google",
        "/health",
        "/hangfire"
    };

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

        // Skip CSRF validation for:
        // 1. Safe HTTP methods (GET, HEAD, OPTIONS, TRACE)
        // 2. Exempt paths
        // 3. API endpoints using JWT authentication (Bearer token)
        if (!ProtectedMethods.Contains(method) ||
            ExemptPaths.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase)) ||
            IsJwtAuthenticated(context))
        {
            await _next(context);
            return;
        }

        // Validate anti-forgery token for state-changing operations
        try
        {
            await _antiforgery.ValidateRequestAsync(context);
            _logger.LogDebug("CSRF token validated successfully for {Path}", path);
        }
        catch (AntiforgeryValidationException ex)
        {
            _logger.LogWarning(ex, "CSRF validation failed for {Path}", path);
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
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

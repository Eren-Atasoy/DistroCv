namespace DistroCv.Api.Middleware;

/// <summary>
/// Middleware to add security headers to all HTTP responses
/// Implements OWASP security best practices
/// </summary>
public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SecurityHeadersMiddleware> _logger;
    private readonly IWebHostEnvironment _environment;

    public SecurityHeadersMiddleware(
        RequestDelegate next,
        ILogger<SecurityHeadersMiddleware> logger,
        IWebHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Add security headers before processing the request
        AddSecurityHeaders(context);

        await _next(context);
    }

    private void AddSecurityHeaders(HttpContext context)
    {
        var headers = context.Response.Headers;

        // 1. HTTP Strict Transport Security (HSTS)
        // Forces HTTPS connections for 1 year, including subdomains
        if (!_environment.IsDevelopment())
        {
            headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains; preload";
        }

        // 2. Content Security Policy (CSP)
        // Prevents XSS attacks by controlling resource loading
        var cspDirectives = new[]
        {
            "default-src 'self'",
            "script-src 'self' 'unsafe-inline' 'unsafe-eval'", // Consider removing unsafe-* in production
            "style-src 'self' 'unsafe-inline' https://fonts.googleapis.com",
            "img-src 'self' data: https:",
            "font-src 'self' https://fonts.gstatic.com",
            "connect-src 'self' https://api.gemini.google.com wss:",
            "frame-ancestors 'none'",
            "base-uri 'self'",
            "form-action 'self'",
            "upgrade-insecure-requests"
        };
        headers["Content-Security-Policy"] = string.Join("; ", cspDirectives);

        // 3. X-Frame-Options
        // Prevents clickjacking attacks
        headers["X-Frame-Options"] = "DENY";

        // 4. X-Content-Type-Options
        // Prevents MIME type sniffing
        headers["X-Content-Type-Options"] = "nosniff";

        // 5. X-XSS-Protection
        // Enables browser's XSS filter (legacy browsers)
        headers["X-XSS-Protection"] = "1; mode=block";

        // 6. Referrer-Policy
        // Controls referrer information sent with requests
        headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

        // 7. Permissions-Policy (formerly Feature-Policy)
        // Controls browser features and APIs
        var permissionsPolicy = new[]
        {
            "accelerometer=()",
            "camera=()",
            "geolocation=()",
            "gyroscope=()",
            "magnetometer=()",
            "microphone=()",
            "payment=()",
            "usb=()"
        };
        headers["Permissions-Policy"] = string.Join(", ", permissionsPolicy);

        // 8. X-Permitted-Cross-Domain-Policies
        // Restricts Adobe Flash and PDF cross-domain requests
        headers["X-Permitted-Cross-Domain-Policies"] = "none";

        // 9. X-Download-Options
        // Prevents IE from executing downloads in site's context
        headers["X-Download-Options"] = "noopen";

        // 10. Cache-Control for sensitive endpoints
        if (IsSensitiveEndpoint(context.Request.Path))
        {
            headers["Cache-Control"] = "no-store, no-cache, must-revalidate, private";
            headers["Pragma"] = "no-cache";
            headers["Expires"] = "0";
        }

        // 11. Remove server information headers
        headers.Remove("Server");
        headers.Remove("X-Powered-By");
        headers.Remove("X-AspNet-Version");
        headers.Remove("X-AspNetMvc-Version");

        _logger.LogDebug("Security headers added to response for {Path}", context.Request.Path);
    }

    /// <summary>
    /// Determine if endpoint contains sensitive data
    /// </summary>
    private static bool IsSensitiveEndpoint(PathString path)
    {
        var sensitivePaths = new[]
        {
            "/api/auth",
            "/api/profile",
            "/api/applications",
            "/api/dashboard"
        };

        return sensitivePaths.Any(p => path.StartsWithSegments(p, StringComparison.OrdinalIgnoreCase));
    }
}

/// <summary>
/// Extension method to add security headers middleware
/// </summary>
public static class SecurityHeadersMiddlewareExtensions
{
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<SecurityHeadersMiddleware>();
    }
}

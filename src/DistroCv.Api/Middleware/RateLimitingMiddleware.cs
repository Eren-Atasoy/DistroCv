using System.Collections.Concurrent;
using System.Net;

namespace DistroCv.Api.Middleware;

/// <summary>
/// Middleware for rate limiting API requests
/// Implements sliding window rate limiting per IP address
/// </summary>
public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RateLimitingMiddleware> _logger;
    
    // Store request timestamps per IP address
    private static readonly ConcurrentDictionary<string, Queue<DateTime>> RequestLog = new();
    
    // Rate limit configuration
    private const int MaxRequestsPerWindow = 100; // Maximum requests per time window
    private static readonly TimeSpan TimeWindow = TimeSpan.FromMinutes(1); // Time window duration
    private static readonly TimeSpan CleanupInterval = TimeSpan.FromMinutes(5); // Cleanup old entries
    private static DateTime _lastCleanup = DateTime.UtcNow;

    // Paths exempt from rate limiting
    private static readonly HashSet<string> ExemptPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "/health",
        "/",
        "/swagger"
    };

    public RateLimitingMiddleware(
        RequestDelegate next,
        ILogger<RateLimitingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? string.Empty;

        // Skip rate limiting for exempt paths
        if (ExemptPaths.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
        {
            await _next(context);
            return;
        }

        // Get client IP address
        var clientIp = GetClientIpAddress(context);

        // Perform periodic cleanup
        PerformCleanup();

        // Get or create request queue for this IP
        var requestQueue = RequestLog.GetOrAdd(clientIp, _ => new Queue<DateTime>());

        bool rateLimitExceeded = false;
        int currentCount = 0;
        
        lock (requestQueue)
        {
            var now = DateTime.UtcNow;
            var windowStart = now - TimeWindow;

            // Remove requests outside the current time window
            while (requestQueue.Count > 0 && requestQueue.Peek() < windowStart)
            {
                requestQueue.Dequeue();
            }

            currentCount = requestQueue.Count;
            
            // Check if rate limit exceeded
            if (currentCount >= MaxRequestsPerWindow)
            {
                rateLimitExceeded = true;
            }
            else
            {
                // Add current request to queue
                requestQueue.Enqueue(now);
            }
        }
        
        // Handle rate limit exceeded outside of lock
        if (rateLimitExceeded)
        {
            _logger.LogWarning(
                "Rate limit exceeded for IP {ClientIp}. Requests: {Count}/{Max}",
                clientIp, currentCount, MaxRequestsPerWindow);

            context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
            context.Response.Headers["Retry-After"] = TimeWindow.TotalSeconds.ToString();
            
            await context.Response.WriteAsJsonAsync(new
            {
                error = "Rate limit exceeded",
                message = $"Too many requests. Maximum {MaxRequestsPerWindow} requests per {TimeWindow.TotalMinutes} minute(s) allowed.",
                retryAfter = TimeWindow.TotalSeconds
            });
            return;
        }

        // Add rate limit headers
        context.Response.Headers["X-RateLimit-Limit"] = MaxRequestsPerWindow.ToString();
        context.Response.Headers["X-RateLimit-Remaining"] = (MaxRequestsPerWindow - requestQueue.Count).ToString();
        context.Response.Headers["X-RateLimit-Reset"] = (DateTime.UtcNow.Add(TimeWindow).ToUnixTimeSeconds()).ToString();

        await _next(context);
    }

    /// <summary>
    /// Get client IP address from request
    /// Handles X-Forwarded-For header for proxied requests
    /// </summary>
    private static string GetClientIpAddress(HttpContext context)
    {
        // Check for forwarded IP first (in case of proxy/load balancer)
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            return forwardedFor.Split(',')[0].Trim();
        }

        // Fall back to remote IP
        return context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
    }

    /// <summary>
    /// Periodically clean up old entries to prevent memory leaks
    /// </summary>
    private static void PerformCleanup()
    {
        var now = DateTime.UtcNow;
        if (now - _lastCleanup < CleanupInterval)
        {
            return;
        }

        _lastCleanup = now;
        var cutoff = now - TimeWindow - TimeWindow; // Keep extra buffer

        // Remove IPs with no recent requests
        var keysToRemove = RequestLog
            .Where(kvp =>
            {
                lock (kvp.Value)
                {
                    return kvp.Value.Count == 0 || kvp.Value.All(t => t < cutoff);
                }
            })
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in keysToRemove)
        {
            RequestLog.TryRemove(key, out _);
        }
    }
}

/// <summary>
/// Extension method to add rate limiting middleware
/// </summary>
public static class RateLimitingMiddlewareExtensions
{
    public static IApplicationBuilder UseRateLimiting(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<RateLimitingMiddleware>();
    }
}

/// <summary>
/// Extension method for DateTime to Unix timestamp
/// </summary>
public static class DateTimeExtensions
{
    public static long ToUnixTimeSeconds(this DateTime dateTime)
    {
        return new DateTimeOffset(dateTime).ToUnixTimeSeconds();
    }
}

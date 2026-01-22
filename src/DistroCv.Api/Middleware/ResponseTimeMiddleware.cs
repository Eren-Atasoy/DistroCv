using System.Diagnostics;

namespace DistroCv.Api.Middleware;

/// <summary>
/// Middleware to track and log API response times (Task 29.5)
/// Target: Response time < 2 seconds (Requirement 15.3)
/// </summary>
public class ResponseTimeMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ResponseTimeMiddleware> _logger;
    private const int TargetResponseTimeMs = 2000; // 2 seconds target

    public ResponseTimeMiddleware(RequestDelegate next, ILogger<ResponseTimeMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();

        // Add response time header
        context.Response.OnStarting(() =>
        {
            stopwatch.Stop();
            var responseTime = stopwatch.ElapsedMilliseconds;
            
            context.Response.Headers["X-Response-Time"] = $"{responseTime}ms";
            
            // Log warning if response time exceeds target
            if (responseTime > TargetResponseTimeMs)
            {
                _logger.LogWarning(
                    "Slow API response detected: {Method} {Path} took {ElapsedMs}ms (target: {TargetMs}ms)",
                    context.Request.Method,
                    context.Request.Path,
                    responseTime,
                    TargetResponseTimeMs);
            }
            else
            {
                _logger.LogDebug(
                    "API response: {Method} {Path} completed in {ElapsedMs}ms",
                    context.Request.Method,
                    context.Request.Path,
                    responseTime);
            }

            return Task.CompletedTask;
        });

        await _next(context);
    }
}

/// <summary>
/// Extension method for registering response time middleware
/// </summary>
public static class ResponseTimeMiddlewareExtensions
{
    public static IApplicationBuilder UseResponseTimeTracking(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ResponseTimeMiddleware>();
    }
}


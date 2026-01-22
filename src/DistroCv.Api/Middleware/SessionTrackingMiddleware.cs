using DistroCv.Core.Interfaces;
using System.Security.Claims;

namespace DistroCv.Api.Middleware;

/// <summary>
/// Middleware to track user session activity
/// </summary>
public class SessionTrackingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SessionTrackingMiddleware> _logger;

    public SessionTrackingMiddleware(RequestDelegate next, ILogger<SessionTrackingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, ISessionService sessionService)
    {
        // Only track authenticated requests
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var accessToken = context.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            
            if (!string.IsNullOrEmpty(accessToken))
            {
                try
                {
                    // Update session activity asynchronously without blocking the request
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await sessionService.UpdateSessionActivityAsync(accessToken);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error updating session activity");
                        }
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in session tracking middleware");
                }
            }
        }

        await _next(context);
    }
}

/// <summary>
/// Extension method to add session tracking middleware
/// </summary>
public static class SessionTrackingMiddlewareExtensions
{
    public static IApplicationBuilder UseSessionTracking(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<SessionTrackingMiddleware>();
    }
}

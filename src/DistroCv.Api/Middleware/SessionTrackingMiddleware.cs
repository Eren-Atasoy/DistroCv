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
    private readonly IServiceScopeFactory _scopeFactory;

    public SessionTrackingMiddleware(
        RequestDelegate next,
        ILogger<SessionTrackingMiddleware> logger,
        IServiceScopeFactory scopeFactory)
    {
        _next = next;
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Only track authenticated requests
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var accessToken = context.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");

            if (!string.IsNullOrEmpty(accessToken))
            {
                // Fire-and-forget with its own DI scope so it doesn't share
                // the request-scoped DbContext with the controller pipeline.
                _ = Task.Run(async () =>
                {
                    try
                    {
                        using var scope = _scopeFactory.CreateScope();
                        var sessionService = scope.ServiceProvider.GetRequiredService<ISessionService>();
                        await sessionService.UpdateSessionActivityAsync(accessToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error updating session activity");
                    }
                });
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

using Hangfire.Dashboard;

namespace DistroCv.Api.Middleware;

/// <summary>
/// Authorization filter for Hangfire Dashboard
/// In development, allows all access. In production, should be restricted.
/// </summary>
public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        // In development, allow all access
        // In production, implement proper authorization
        var httpContext = context.GetHttpContext();
        return httpContext.Request.Host.Host == "localhost" || 
               httpContext.Request.Host.Host == "127.0.0.1";
    }
}

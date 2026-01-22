using Microsoft.Extensions.DependencyInjection;

namespace DistroCv.Infrastructure.Gmail;

/// <summary>
/// Extension methods for Gmail service registration
/// </summary>
public static class GmailServiceExtensions
{
    /// <summary>
    /// Adds Gmail services to the service collection
    /// </summary>
    public static IServiceCollection AddGmailServices(this IServiceCollection services)
    {
        services.AddScoped<IGmailService, GmailService>();
        
        return services;
    }
}

using DistroCv.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DistroCv.Infrastructure.Gemini;

/// <summary>
/// Extension methods for registering Gemini services
/// </summary>
public static class GeminiServiceExtensions
{
    /// <summary>
    /// Adds Gemini services to the service collection
    /// </summary>
    public static IServiceCollection AddGeminiServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Register configuration
        services.Configure<GeminiConfiguration>(
            configuration.GetSection(GeminiConfiguration.SectionName));

        // Register HttpClient for Gemini
        services.AddHttpClient<IGeminiService, GeminiService>()
            .ConfigureHttpClient((sp, client) =>
            {
                client.Timeout = TimeSpan.FromSeconds(60);
            });

        return services;
    }
}

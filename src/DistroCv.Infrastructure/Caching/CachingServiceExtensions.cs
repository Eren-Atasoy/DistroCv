using DistroCv.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DistroCv.Infrastructure.Caching;

/// <summary>
/// Extension methods for registering caching services (Task 29.1)
/// </summary>
public static class CachingServiceExtensions
{
    /// <summary>
    /// Adds caching services to the DI container
    /// Uses Redis if configured, falls back to in-memory cache
    /// </summary>
    public static IServiceCollection AddCachingServices(this IServiceCollection services, IConfiguration configuration)
    {
        var redisConnectionString = configuration.GetConnectionString("Redis");

        if (!string.IsNullOrEmpty(redisConnectionString))
        {
            // Use Redis distributed cache
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisConnectionString;
                options.InstanceName = "DistroCv:";
            });

            services.AddScoped<ICacheService, RedisCacheService>();
        }
        else
        {
            // Fall back to in-memory cache for development
            services.AddMemoryCache();
            services.AddDistributedMemoryCache();
            services.AddScoped<ICacheService, InMemoryCacheService>();
        }

        return services;
    }

    /// <summary>
    /// Adds cached version of matching service (decorator pattern)
    /// </summary>
    public static IServiceCollection AddCachedMatchingService(this IServiceCollection services)
    {
        // Decorate IMatchingService with cached version
        services.Decorate<IMatchingService, Services.CachedMatchingService>();
        return services;
    }
}

/// <summary>
/// Extension to support decorator pattern
/// </summary>
public static class DecoratorExtensions
{
    /// <summary>
    /// Decorates a service with another implementation
    /// </summary>
    public static IServiceCollection Decorate<TInterface, TDecorator>(this IServiceCollection services)
        where TInterface : class
        where TDecorator : class, TInterface
    {
        var wrappedDescriptor = services.FirstOrDefault(s => s.ServiceType == typeof(TInterface));

        if (wrappedDescriptor == null)
            throw new InvalidOperationException($"Service {typeof(TInterface).Name} is not registered");

        var objectFactory = ActivatorUtilities.CreateFactory(
            typeof(TDecorator),
            new[] { typeof(TInterface) });

        services.Add(ServiceDescriptor.Describe(
            typeof(TInterface),
            sp => (TInterface)objectFactory(sp, new[] { CreateInstance(sp, wrappedDescriptor) }),
            wrappedDescriptor.Lifetime));

        return services;
    }

    private static object CreateInstance(IServiceProvider sp, ServiceDescriptor descriptor)
    {
        if (descriptor.ImplementationInstance != null)
            return descriptor.ImplementationInstance;

        if (descriptor.ImplementationFactory != null)
            return descriptor.ImplementationFactory(sp);

        return ActivatorUtilities.GetServiceOrCreateInstance(sp, descriptor.ImplementationType!);
    }
}


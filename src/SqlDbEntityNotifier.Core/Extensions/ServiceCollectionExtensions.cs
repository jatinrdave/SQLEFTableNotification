using Microsoft.Extensions.DependencyInjection;
using SqlDbEntityNotifier.Core.Interfaces;
using SqlDbEntityNotifier.Core.Serializers;

namespace SqlDbEntityNotifier.Core.Extensions;

/// <summary>
/// Extension methods for configuring SQLDBEntityNotifier services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds SQLDBEntityNotifier core services to the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional configuration action.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddSqlDbEntityNotifier(
        this IServiceCollection services,
        Action<SqlDbEntityNotifierOptions>? configure = null)
    {
        if (configure != null)
        {
            services.Configure(configure);
        }

        // Register default serializer
        services.AddSingleton<ISerializer, JsonSerializer>();

        return services;
    }

    /// <summary>
    /// Adds a database adapter to the service collection.
    /// </summary>
    /// <typeparam name="TAdapter">The adapter type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="lifetime">The service lifetime.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddDbAdapter<TAdapter>(
        this IServiceCollection services,
        ServiceLifetime lifetime = ServiceLifetime.Singleton)
        where TAdapter : class, IDbAdapter
    {
        services.Add(new ServiceDescriptor(typeof(IDbAdapter), typeof(TAdapter), lifetime));
        return services;
    }

    /// <summary>
    /// Adds a change publisher to the service collection.
    /// </summary>
    /// <typeparam name="TPublisher">The publisher type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="lifetime">The service lifetime.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddChangePublisher<TPublisher>(
        this IServiceCollection services,
        ServiceLifetime lifetime = ServiceLifetime.Singleton)
        where TPublisher : class, IChangePublisher
    {
        services.Add(new ServiceDescriptor(typeof(IChangePublisher), typeof(TPublisher), lifetime));
        return services;
    }
}
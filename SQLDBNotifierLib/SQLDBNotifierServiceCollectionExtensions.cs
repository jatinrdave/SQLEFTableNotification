using Microsoft.Extensions.DependencyInjection;

namespace SQLDBEntityNotifier
{
    /// <summary>
    /// Extension methods for registering SQLDBNotifier services in DI.
    /// </summary>
    public static class SQLDBNotifierServiceCollectionExtensions
    {
        public static IServiceCollection AddSQLDBNotifier(this IServiceCollection services)
        {
            services.AddScoped(typeof(IChangeTableService<>), typeof(ChangeTableService<>));
            services.AddScoped(typeof(IDBNotificationService<>), typeof(SqlDBNotificationService<>));
            services.AddScoped<ISQLTableMonitorManager, SQLTableMonitorManager>();
            return services;
        }
    }
}

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PostgreSQL.ListenNotify.Configuration;
using System;

namespace PostgreSQL.ListenNotify.DependencyInjection
{
    /// <summary>
    /// Extension methods for adding PostgreSQL.ListenNotify services to the dependency injection container
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds PostgreSQL notification services to the specified IServiceCollection
        /// </summary>
        /// <param name="services">The IServiceCollection to add services to</param>
        /// <param name="configureOptions">Configuration for the notification service</param>
        /// <returns>The IServiceCollection for further configuration</returns>
        public static IServiceCollection AddPostgresNotifications(
            this IServiceCollection services,
            Action<PostgresNotificationOptions> configureOptions)
        {
            if (configureOptions == null)
                throw new ArgumentNullException(nameof(configureOptions));

            // Ensure logging services are registered
            services.AddLogging();

            var options = new PostgresNotificationOptions();
            configureOptions(options);
            options.Validate();

            services.AddSingleton(options);
            
            // Register the service directly as both IPostgresNotificationService and IHostedService
            services.AddSingleton<PostgresNotificationService>();
            services.AddSingleton<IPostgresNotificationService>(sp => 
                sp.GetRequiredService<PostgresNotificationService>());
            services.AddHostedService(sp => 
                sp.GetRequiredService<PostgresNotificationService>());

            return services;
        }
    }
}
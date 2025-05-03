using System;
using System.Reflection;
using MassTransit;
using MassTransit.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Play.Common.Settings;

namespace Play.Common.MassTransit
{
    public static class Extensions
    {
        public static IServiceCollection AddMassTransitWithRabbitMq(
            this IServiceCollection services,
            Action<IRetryConfigurator> configureRetries = null)
        {
            services.AddMassTransit(configure =>
            {
                configure.AddConsumers(Assembly.GetEntryAssembly());
                configure.UsingPlayEconomyRabbitMQ(configureRetries);
            });

            // services.AddMassTransitHostedService();
            return services;
        }

        public static void UsingPlayEconomyRabbitMQ(
            this IBusRegistrationConfigurator configure,
            Action<IRetryConfigurator> configureRetries = null)
        {
            configure.UsingRabbitMq((context, configurator) =>
            {
                var configuration = context.GetService<IConfiguration>();
                var serviceSettings = configuration.GetSection(nameof(ServiceSettings)).Get<ServiceSettings>();
                var rabbitMQSettings = configuration.GetSection(nameof(RabbitMQSettings)).Get<RabbitMQSettings>();

                configurator.Host(rabbitMQSettings.Host);
                configurator.ConfigureEndpoints(
                    context,
                    new KebabCaseEndpointNameFormatter(serviceSettings.ServiceName, false));

                if (configureRetries == null)
                {
                    configureRetries = (retryConfigurator) => retryConfigurator.Interval(3, TimeSpan.FromSeconds(5));
                }

                configurator.UseMessageRetry(configureRetries);
            });
        }
    }
}
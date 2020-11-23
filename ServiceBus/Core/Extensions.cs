#region Header
// //-----------------------------------------------------------------------
// // <copyright file="Extensions.cs" company="INVENTIO AG">
// //     Copyright © 2020 INVENTIO AG
// //     All rights reserved.
// //     INVENTIO AG, Seestrasse 55, CH-6052 Hergiswil, owns and retains all copyrights and other intellectual property rights in this
// //     document. Any reproduction, translation, copying or storing in data processing units in any form or by any means without prior
// //     permission of INVENTIO AG is regarded as infringement and will be prosecuted.
// //
// //     'CONFIDENTIAL'
// //     This document contains confidential information that is proprietary to the Schindler Group. Neither this document nor the
// //     information contained herein shall be disclosed to third parties nor used for manufacturing or any other application without
// //     written consent of INVENTIO AG.
// // </copyright>
// //-----------------------------------------------------------------------
#endregion

using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.NamedDependencyInjection;

namespace Core.ServiceBus
{
    public static class Extensions
    {
        public static IServiceCollection AddEventConsumer(this IServiceCollection services, IConfiguration configuration, string topicName)
        {
            if (services is null)
            {
                throw new ArgumentNullException(nameof(services));
            }
            if (configuration is null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }
            if (string.IsNullOrWhiteSpace(topicName))
            {
                throw new ArgumentException("Should not be empty", nameof(topicName));
            }

            services.Scan(scan => scan.FromEntryAssembly().AddClasses(classes => classes.AssignableTo(typeof(IIntegrationEventHandler<>))).AsSelf().WithScopedLifetime());

            EventBusOptions options = configuration.GetEventBusOptions();
            options.Topic = topicName;

            if (options.UseAzureServiceBus)
            {
                services.AddSingleton<IEventConsumer, ServiceBusConsumer, string>(factory =>
                        new ServiceBusConsumer(
                            options,
                            factory.GetRequiredService<EventBusSubscriptionsManager>(),
                            factory,
                            factory.GetRequiredService<ILogger<ServiceBusConsumer>>()),
                    topicName);
            }
            else
            {
                //services.AddSingleton<IEventConsumer, RabbitMqConsumer, string>(factory =>
                //    new RabbitMqConsumer(options, factory.GetService<ILogger<RabbitMqConsumer>>(), factory),
                //    topicName);
            }

            return services;
        }

        public static IEventConsumer GetConsumer(this IServiceProvider serviceProvider, string topicName)
        {
            if (serviceProvider is null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }
            if (string.IsNullOrWhiteSpace(topicName))
            {
                throw new ArgumentException("Should not be empty", nameof(topicName));
            }
            return serviceProvider.GetRequiredService<IEventConsumer, string>(topicName);
        }

        private static EventBusOptions GetEventBusOptions(this IConfiguration configuration)
        {
            const string sectionName = "EventBus";
            EventBusOptions options = configuration.GetSection(sectionName).Get<EventBusOptions>();
            if (options is null)
            {
                throw new InvalidOperationException($"Section '{sectionName}' is not present in configuration file");
            }

            return options;
        }
    }
}

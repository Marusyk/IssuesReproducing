using Core.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;

namespace ServiceBus
{
    public static class Program
    {
        public static void Main()
        {
            Host.CreateDefaultBuilder()
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddEventConsumer(hostContext.Configuration, Topic.EdgeDeviceData);
                    services.AddEventConsumer(hostContext.Configuration, Topic.EdgeDevice);

                    services.AddHostedService<EventConsumerService>();
                })
                .Build()
                .Run();
        }
    }
}

using Core.ServiceBus;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceBus
{
    internal static class Topic
    {
        internal const string EdgeDeviceData = "edge-device-data";
        internal const string EdgeDevice = "edge-device";
    }

    internal class EventConsumerService : BackgroundService
    {
        private readonly IEventConsumer _edgeDeviceDataEventsConsumer;
        private readonly IEventConsumer _edgeDeviceEventsConsumer;

        public EventConsumerService(IServiceProvider serviceProvider)
        {
            _edgeDeviceDataEventsConsumer = serviceProvider.GetConsumer(Topic.EdgeDeviceData);
            _edgeDeviceEventsConsumer = serviceProvider.GetConsumer(Topic.EdgeDevice);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _edgeDeviceDataEventsConsumer.Subscribe<DeviceDescriptionEvent, DeviceDescriptionEventHandler>("DeviceDescription");
            _edgeDeviceEventsConsumer.Subscribe<DeviceConnectionStateChangedEvent, DeviceConnectionStateChangedEventHandler>("DeviceConnectionStateChanged");

            return Task.CompletedTask;
        }
    }
}

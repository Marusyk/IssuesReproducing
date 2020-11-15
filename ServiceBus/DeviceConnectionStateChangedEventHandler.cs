using Core.ServiceBus;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace ServiceBus
{
    public class DeviceConnectionStateChangedEventHandler : IIntegrationEventHandler<DeviceConnectionStateChangedEvent>
    {
        private readonly ILogger<DeviceConnectionStateChangedEventHandler> _logger;

        public DeviceConnectionStateChangedEventHandler(ILogger<DeviceConnectionStateChangedEventHandler> logger)
        {
            _logger = logger;
        }

        public Task Handle(DeviceConnectionStateChangedEvent eventData)
        {
            _logger.LogInformation("Device {DeviceId} connected: {IsConnected}", eventData.DeviceId, eventData.Connected);
            // ... precessing the event
            // go to database and other business logic
            return Task.CompletedTask;
        }
    }
}

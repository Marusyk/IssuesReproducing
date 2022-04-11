using Core.ServiceBus;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace ServiceBus
{
    public class DeviceDescriptionEventHandler : IIntegrationEventHandler<DeviceDescriptionEvent>
    {
        private readonly ILogger<DeviceDescriptionEventHandler> _logger;

        public DeviceDescriptionEventHandler(ILogger<DeviceDescriptionEventHandler> logger)
        {
            _logger = logger;
        }

        public Task Handle(DeviceDescriptionEvent eventData)
        {
            _logger.LogInformation("Device {DeviceId}", eventData.DeviceId);
            // ... precessing the event
            // go to database and other business logic
            return Task.CompletedTask;
        }
    }
}

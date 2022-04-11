using Core.ServiceBus;

namespace ServiceBus
{
    public class DeviceConnectionStateChangedEvent : IIntegrationEvent
    {
        public string DeviceId { get; set; }
        public bool Connected { get; set; }
    }
}

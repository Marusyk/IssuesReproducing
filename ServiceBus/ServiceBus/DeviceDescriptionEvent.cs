using Core.ServiceBus;

namespace ServiceBus
{
    public class DeviceDescriptionEvent : IIntegrationEvent
    {
        public string GroupId { get; set; }
        public string DeviceId { get; set; }
        public string HealthState { get; set; }
        public string GenericState { get; set; }
    }
}

using System;
using Newtonsoft.Json;

namespace Core.ServiceBus
{
    [Serializable]
    public class IntegrationEvent : IIntegrationEvent
    {
        protected IntegrationEvent(string eventType)
        {
            if (string.IsNullOrEmpty(eventType))
            {
                throw new ArgumentNullException(nameof(eventType));
            }

            EventType = eventType;
            Id = Guid.NewGuid();
            CreateDateTime = DateTime.UtcNow;
        }

        protected IntegrationEvent(string eventType, string key)
            : this(eventType)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentNullException(nameof(key));
            }

            Key = key;
        }

        [JsonProperty]
        public Guid Id { get; private set; }

        [JsonProperty]
        public DateTime CreateDateTime { get; private set; }

        [JsonProperty]
        public string EventType { get; private set; }

        [JsonProperty]
        public virtual string Key { get; protected set; }
    }
}

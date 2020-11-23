using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Core.ServiceBus
{
    public sealed class ServiceBusProducer : IEventProducer
    {
        private readonly ServiceBusProducerConnection _connection;
        private readonly ILogger _logger;

        private static readonly JsonSerializerSettings _jsonSerializerSettings = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };

        public ServiceBusProducer(EventBusOptions options, ILogger<ServiceBusProducer> logger)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _connection = new ServiceBusProducerConnection(options, logger);
        }

        public Task Send(IntegrationEvent eventData)
        {
            var jsonMessage = JsonConvert.SerializeObject(eventData, _jsonSerializerSettings);
            byte[] body = Encoding.UTF8.GetBytes(jsonMessage);

            var message = new Message
            {
                MessageId = eventData.Id.ToString(),
                Body = body,
                Label = eventData.EventType,
                ContentType = "application/json",
                SessionId = eventData.Key ?? eventData.EventType
            };

            _logger.LogInformation("Sending event message {EventType} to topic {TopicName} with ID:'{MessageId}'",
                eventData.EventType, _connection.TopicClient.TopicName, eventData.Id);

            return _connection.TopicClient.SendAsync(message);
        }
    }
}

using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus.Management;

namespace Core.ServiceBus
{
    public sealed class ServiceBusProducer : IEventProducer, IDisposable
    {
        private bool _disposed;
        private readonly ITopicClient _topicClient;
        private readonly ILogger _logger;

        public ServiceBusProducer(EventBusOptions options, ILogger<ServiceBusProducer> logger)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            CreateTopicIfNotExists(options);
            _topicClient = new TopicClient(options.ConnectionString, options.Topic, RetryPolicy.Default);
        }

        public Task Send(IntegrationEvent eventData)
        {
            var jsonMessage = JsonConvert.SerializeObject(eventData, new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            });
            byte[] body = Encoding.UTF8.GetBytes(jsonMessage);

            var message = new Message
            {
                MessageId = eventData.Id.ToString(),
                Body = body,
                Label = eventData.EventType,
                ContentType = "application/json",
                SessionId = eventData.Key ?? eventData.EventType
            };

            _logger.LogDebug("Sending event message {EventType} to topic {TopicName}. Payload: {Payload}", eventData.EventType, _topicClient.TopicName, jsonMessage);
            return _topicClient.SendAsync(message);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public static void CreateTopicIfNotExists(EventBusOptions options)
        {
            var managementClient = new ManagementClient(options.ConnectionString);
            var exists = managementClient.TopicExistsAsync(options.Topic).GetAwaiter().GetResult();
            if (!exists)
            {
                managementClient.CreateTopicAsync(options.Topic).GetAwaiter().GetResult();
            }
        }

        private void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing && _topicClient != null && !_topicClient.IsClosedOrClosing)
            {
                _logger.LogInformation("Closing Service Bus connection to topic '{TopicName}'", _topicClient.TopicName);
                _topicClient.CloseAsync().GetAwaiter().GetResult();
            }

            _disposed = true;
        }
    }
}

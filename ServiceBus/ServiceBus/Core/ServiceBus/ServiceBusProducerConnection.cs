using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Management;
using Microsoft.Extensions.Logging;
using System;

namespace Core.ServiceBus
{
    internal sealed class ServiceBusProducerConnection : IDisposable
    {
        private bool _disposed;
        private readonly EventBusOptions _options;
        private readonly ILogger _logger;
        private ITopicClient _topicClient;

        public ServiceBusProducerConnection(EventBusOptions options, ILogger logger)
        {
            _options = options;
            _logger = logger;

            _topicClient = new TopicClient(options.ConnectionString, _options.Topic, RetryPolicy.Default);
        }

        public ITopicClient TopicClient {
            get {
                if (_topicClient.IsClosedOrClosing)
                {
                    _topicClient = new TopicClient(_options.ConnectionString, _options.Topic, RetryPolicy.Default);
                }

                return _topicClient;
            }
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

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
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

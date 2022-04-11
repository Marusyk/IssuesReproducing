
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Management;
using Microsoft.Extensions.Logging;

namespace Core.ServiceBus
{
    internal sealed class ServiceBusConsumerConnection : IDisposable
    {
        private bool _disposed;
        private readonly EventBusOptions _options;
        private readonly ILogger _logger;
        private ISubscriptionClient _subscriptionClient;

        internal ServiceBusConsumerConnection(EventBusOptions options, ILogger logger)
        {
            _options = options;
            _logger = logger;

            CreateSubscriptionIfNotExists();
            _subscriptionClient = new SubscriptionClient(_options.ConnectionString, _options.Topic, _options.Subscription, ReceiveMode.PeekLock, RetryPolicy.Default);
            RemoveDefaultRule();
            PrintSubscriptionInfo();
        }

        internal void RegisterHandler(Func<Message, Task<bool>> messageHandler)
        {
            if (messageHandler == null)
            {
                throw new ArgumentNullException(nameof(messageHandler));
            }

            if (_options.UseSessions)
            {
                RegisterSessionHandler(messageHandler);
            }
            else
            {
                RegisterMessageHandler(messageHandler);
            }
        }

        internal ISubscriptionClient SubscriptionClient {
            get {
                if (_subscriptionClient.IsClosedOrClosing)
                {
                    _subscriptionClient = new SubscriptionClient(_options.ConnectionString, _options.Topic, _options.Subscription, ReceiveMode.PeekLock, RetryPolicy.Default);
                }

                return _subscriptionClient;
            }
        }

        private void RegisterMessageHandler(Func<Message, Task<bool>> messageHandler)
        {
            _logger.LogInformation("Register message handler for subscription {Subscription} in topic {TopicPath}",
                _options.Subscription,
                SubscriptionClient.TopicPath);

            var options = new MessageHandlerOptions(ExceptionReceivedHandler)
            {
                MaxConcurrentCalls = _options.MaxConcurrentCalls,
                AutoComplete = false
            };
            SubscriptionClient.RegisterMessageHandler(async (message, ct) =>
            {
                if (await messageHandler(message) && !ct.IsCancellationRequested)
                {
                    await SubscriptionClient.CompleteAsync(message.SystemProperties.LockToken);
                }
            }, options);
        }

        private void RegisterSessionHandler(Func<Message, Task<bool>> messageHandler)
        {
            _logger.LogInformation("Register session handler for subscription {Subscription} in topic {TopicPath}",
                _options.Subscription,
                SubscriptionClient.TopicPath);

            var options = new SessionHandlerOptions(ExceptionReceivedHandler)
            {
                MaxConcurrentSessions = _options.MaxConcurrentCalls,
                AutoComplete = false
            };
            SubscriptionClient.RegisterSessionHandler(async (session, message, ct) =>
            {
                if (await messageHandler(message) && !ct.IsCancellationRequested)
                {
                    await session.CompleteAsync(message.SystemProperties.LockToken);
                }
            }, options);
        }

        private void CreateSubscriptionIfNotExists()
        {
            var managementClient = new ManagementClient(_options.ConnectionString);
            var exists = managementClient.SubscriptionExistsAsync(_options.Topic, _options.Subscription).GetAwaiter().GetResult();
            if (!exists)
            {
                _logger.LogInformation("Creating subscription {Subscription} in Topic {TopicName}", _options.Subscription, _options.Topic);
                managementClient.CreateSubscriptionAsync(new SubscriptionDescription(_options.Topic, _options.Subscription)
                {
                    RequiresSession = _options.UseSessions,
                    MaxDeliveryCount = _options.MaxDeliveryCount,
                    LockDuration = TimeSpan.FromSeconds(30),
                    DefaultMessageTimeToLive = TimeSpan.FromDays(1)
                }).GetAwaiter().GetResult();
            }
        }

        private Task ExceptionReceivedHandler(ExceptionReceivedEventArgs exceptionReceivedEventArgs)
        {
            ExceptionReceivedContext context = exceptionReceivedEventArgs.ExceptionReceivedContext;
            _logger.LogError(exceptionReceivedEventArgs.Exception,
                "Service Bus RegisterMessageHandler encountered an exception {Message}. Exception context for troubleshooting: - Endpoint: {Endpoint} - Entity Path: {EntityPath} - Executing Action: {Action}",
                exceptionReceivedEventArgs.Exception.Message, context.Endpoint, context.EntityPath, context.Action);
            return Task.CompletedTask;
        }

        private void RemoveDefaultRule()
        {
            try
            {
                SubscriptionClient
                    .RemoveRuleAsync(RuleDescription.DefaultRuleName)
                    .GetAwaiter()
                    .GetResult();
            }
            catch (MessagingEntityNotFoundException)
            {
                // ignore
            }
        }

        private void PrintSubscriptionInfo()
        {
            try
            {
                IEnumerable<Filter> rules = SubscriptionClient
                    .GetRulesAsync()
                    .GetAwaiter()
                    .GetResult()
                    .Select(x => x.Filter);

                _logger.LogInformation("Subscription '{SubscriptionName}' in topic {TopicName} has rules: {Rules}",
                    SubscriptionClient.SubscriptionName,
                    SubscriptionClient.TopicPath,
                    string.Join("; ", rules));
            }
            catch (MessagingEntityNotFoundException)
            {
                _logger.LogWarning("The subscription {SubscriptionName} could not be found.", SubscriptionClient.SubscriptionName);
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

            if (disposing && _subscriptionClient != null && !_subscriptionClient.IsClosedOrClosing)
            {
                _logger.LogInformation("Closing connection to topic '{TopicName}' and subscription '{SubscriptionName}'",
                    _subscriptionClient.TopicPath,
                    _subscriptionClient.SubscriptionName);

                _subscriptionClient.CloseAsync().GetAwaiter().GetResult();
            }

            _disposed = true;
        }
    }
}

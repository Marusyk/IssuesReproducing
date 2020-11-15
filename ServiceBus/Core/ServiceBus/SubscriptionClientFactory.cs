using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Management;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Core.ServiceBus
{
    internal sealed class SubscriptionClientFactory
    {
        private readonly EventBusOptions _options;
        private readonly ILogger _logger;

        internal SubscriptionClientFactory(EventBusOptions options, ILogger logger)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        internal ISubscriptionClient CreateClient(Func<string, byte[], Task<bool>> eventHandler)
        {
            if (eventHandler == null)
            {
                throw new ArgumentNullException(nameof(eventHandler));
            }

            ISubscriptionClient subscriptionClient = _options.UseSessions ? CreateSessionClient(eventHandler) : CreateMessageClient(eventHandler);
            RemoveDefaultRule(subscriptionClient);
            return subscriptionClient;
        }

        private ISubscriptionClient CreateMessageClient(Func<string, byte[], Task<bool>> eventHandler)
        {
            ISubscriptionClient subscriptionClient = CreateSubscriptionClient();

            _logger.LogInformation("Register message handler for subscription {Subscription} in topic {TopicPath}",
                _options.Subscription,
                subscriptionClient.TopicPath);

            var options = new MessageHandlerOptions(ExceptionReceivedHandler)
            {
                MaxConcurrentCalls = _options.MaxConcurrentCalls,
                AutoComplete = false
            };
            subscriptionClient.RegisterMessageHandler(async (message, ct) =>
            {
                if (await eventHandler(message.Label, message.Body))
                {
                    await subscriptionClient.CompleteAsync(message.SystemProperties.LockToken);
                }
            }, options);
            return subscriptionClient;
        }

        private ISubscriptionClient CreateSessionClient(Func<string, byte[], Task<bool>> eventHandler)
        {
            ISubscriptionClient subscriptionClient = CreateSubscriptionClient();

            _logger.LogInformation("Register session handler for subscription {Subscription} in topic {TopicPath}",
                _options.Subscription,
                subscriptionClient.TopicPath);

            var options = new SessionHandlerOptions(ExceptionReceivedHandler)
            {
                MaxConcurrentSessions = _options.MaxConcurrentCalls,
                AutoComplete = false
            };
            subscriptionClient.RegisterSessionHandler(async (session, message, ct) =>
            {
                if (await eventHandler(message.Label, message.Body))
                {
                    await session.CompleteAsync(message.SystemProperties.LockToken);
                }
            }, options);
            return subscriptionClient;
        }

        private ISubscriptionClient CreateSubscriptionClient()
        {
            CreateSubscriptionIfNotExists();
            return new SubscriptionClient(_options.ConnectionString, _options.Topic, _options.Subscription, ReceiveMode.PeekLock, RetryPolicy.Default);
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

        private static void RemoveDefaultRule(ISubscriptionClient subscriptionClient)
        {
            try
            {
                subscriptionClient
                    .RemoveRuleAsync(RuleDescription.DefaultRuleName)
                    .GetAwaiter()
                    .GetResult();
            }
            catch (MessagingEntityNotFoundException)
            {
                // ignore
            }
        }
    }
}

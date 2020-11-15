using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.ServiceBus
{
    public sealed class ServiceBusConsumer : IEventConsumer, IDisposable
    {
        private bool _disposed;
        private readonly ILogger<ServiceBusConsumer> _logger;
        private readonly IServiceProvider _handlerServiceProvider;
        private readonly EventBusSubscriptionsManager _subscriptionsManager;
        private readonly ISubscriptionClient _subscriptionClient;

        public ServiceBusConsumer(EventBusOptions options, ILogger<ServiceBusConsumer> logger, IServiceProvider handlerServiceProvider)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _handlerServiceProvider = handlerServiceProvider ?? throw new ArgumentNullException(nameof(handlerServiceProvider));

            _subscriptionsManager = new EventBusSubscriptionsManager();
            var clientFactory = new SubscriptionClientFactory(options, logger);
            _subscriptionClient = clientFactory.CreateClient(ProcessEventAsync);

            PrintSubscriptionInfo();
        }

        public void Subscribe<TEvent, TEventHandler>(string eventType)
            where TEvent : IIntegrationEvent
            where TEventHandler : IIntegrationEventHandler<TEvent>
        {
            if (string.IsNullOrWhiteSpace(eventType))
            {
                throw new ArgumentNullException(nameof(eventType));
            }

            bool hasSubscriptions = _subscriptionsManager.HasSubscriptionsForEvent(eventType);
            if (!hasSubscriptions)
            {
                try
                {
                    _subscriptionClient.AddRuleAsync(new RuleDescription
                    {
                        Filter = new CorrelationFilter { Label = eventType },
                        Name = eventType
                    }).GetAwaiter().GetResult();
                }
                catch (ServiceBusException)
                {
                    // ignore
                }
            }

            _logger.LogInformation("Subscribing to event {EventType} with {EventHandler}. Topic: {Topic}",
                eventType,
                typeof(TEventHandler).Name,
                _subscriptionClient.TopicPath);

            _subscriptionsManager.AddSubscription<TEvent, TEventHandler>(eventType);
        }

        public void Unsubscribe<TEvent, TEventHandler>(string eventType)
            where TEvent : IIntegrationEvent
            where TEventHandler : IIntegrationEventHandler<TEvent>
        {
            if (string.IsNullOrEmpty(eventType))
            {
                throw new ArgumentNullException(nameof(eventType));
            }

            try
            {
                _subscriptionClient.RemoveRuleAsync(eventType).GetAwaiter().GetResult();
            }
            catch (MessagingEntityNotFoundException)
            {
                _logger.LogWarning("The messaging entity {EventType} could not be found. Topic: {Topic}", eventType, _subscriptionClient.TopicPath);
            }

            _logger.LogInformation("Unsubscribing from event {EventType}. Topic: {Topic}", eventType, _subscriptionClient.TopicPath);

            _subscriptionsManager.RemoveSubscription<TEvent, TEventHandler>(eventType);
        }

        private async Task<bool> ProcessEventAsync(string eventType, byte[] messageBody)
        {
            bool hasSubscriptions = _subscriptionsManager.HasSubscriptionsForEvent(eventType);
            if (!hasSubscriptions)
            {
                _logger.LogDebug("Ignore message. No subscribers for {EventType} event", eventType);
                return false;
            }

            string message = Encoding.UTF8.GetString(messageBody);

            _logger.LogInformation("Event {EventType} received from {Topic}. Payload: {Payload}", eventType, _subscriptionClient.TopicPath, message);

            using IServiceScope scope = _handlerServiceProvider.CreateScope();
            var subscriptions = _subscriptionsManager.GetSubscriptionsForEvent(eventType);
            try
            {
                foreach (var subscription in subscriptions)
                {
                    var handler = scope.ServiceProvider.GetService(subscription.HandlerType);
                    if (handler == null)
                    {
                        continue;
                    }

                    var integrationEvent = JsonConvert.DeserializeObject(message, subscription.EventType);
                    Type concreteType = typeof(IIntegrationEventHandler<>).MakeGenericType(subscription.EventType);
                    await (Task)concreteType.GetMethod("Handle")?.Invoke(handler, new[] { integrationEvent });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Service Bus message handler encountered an exception {Message}. EventName: {EventName}.", ex.Message, eventType);
                throw;
            }

            return true;
        }

        private void PrintSubscriptionInfo()
        {
            try
            {
                IEnumerable<Filter> rules = _subscriptionClient
                    .GetRulesAsync()
                    .GetAwaiter()
                    .GetResult()
                    .Select(x => x.Filter);

                _logger.LogInformation("Subscription '{SubscriptionName}' in topic {TopicName} has rules: {Rules}",
                    _subscriptionClient.SubscriptionName,
                    _subscriptionClient.TopicPath,
                    string.Join("; ", rules));
            }
            catch (MessagingEntityNotFoundException)
            {
                _logger.LogWarning("The subscription {SubscriptionName} could not be found.", _subscriptionClient.SubscriptionName);
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
                _logger.LogInformation("Closing connection to topic '{TopicName}'", _subscriptionClient.TopicPath);
                _subscriptionClient.CloseAsync().GetAwaiter().GetResult();
            }

            _disposed = true;
        }
    }
}

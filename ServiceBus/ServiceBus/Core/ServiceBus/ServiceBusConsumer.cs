using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Core.ServiceBus
{
    public sealed class ServiceBusConsumer : IEventConsumer
    {
        private readonly EventBusSubscriptionsManager _subscriptionsManager;
        private readonly IServiceProvider _handlerServiceProvider;
        private readonly ServiceBusConsumerConnection _connection;
        private readonly ILogger<ServiceBusConsumer> _logger;

        public ServiceBusConsumer(
            EventBusOptions options,
            EventBusSubscriptionsManager subscriptionsManager,
            IServiceProvider handlerServiceProvider,
            ILogger<ServiceBusConsumer> logger)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            _subscriptionsManager = subscriptionsManager ?? throw new ArgumentNullException(nameof(subscriptionsManager));
            _handlerServiceProvider = handlerServiceProvider ?? throw new ArgumentNullException(nameof(handlerServiceProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _connection = new ServiceBusConsumerConnection(options, logger);
            _connection.RegisterHandler(ProcessMessageAsync);
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
                    _connection.SubscriptionClient.AddRuleAsync(new RuleDescription
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
                _connection.SubscriptionClient.TopicPath);

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
                _connection.SubscriptionClient.RemoveRuleAsync(eventType).GetAwaiter().GetResult();
            }
            catch (MessagingEntityNotFoundException)
            {
                _logger.LogWarning("The messaging entity {EventType} could not be found. Topic: {Topic}", eventType, _connection.SubscriptionClient.TopicPath);
            }

            _logger.LogInformation("Unsubscribing from event {EventType}. Topic: {Topic}", eventType, _connection.SubscriptionClient.TopicPath);

            _subscriptionsManager.RemoveSubscription<TEvent, TEventHandler>(eventType);
        }

        private async Task<bool> ProcessMessageAsync(Message message)
        {
            string eventType = message.Label;
            bool hasSubscriptions = _subscriptionsManager.HasSubscriptionsForEvent(eventType);
            if (!hasSubscriptions)
            {
                _logger.LogInformation("Ignore message. No subscribers for {EventType} event with ID:'{MessageId}'", eventType, message.MessageId);
                return true;
            }

            string jsonPayload = Encoding.UTF8.GetString(message.Body);

            _logger.LogInformation("Event {EventType} received from {Topic} with ID:'{MessageId}'. Payload: {Payload}",
                eventType,
                _connection.SubscriptionClient.TopicPath,
                message.MessageId,
                jsonPayload);

            using IServiceScope scope = _handlerServiceProvider.CreateScope();
            IEnumerable<SubscriptionInfo> subscriptions = _subscriptionsManager.GetSubscriptionsForEvent(eventType);
            try
            {
                foreach (SubscriptionInfo subscription in subscriptions)
                {
                    object handler = scope.ServiceProvider.GetService(subscription.HandlerType);
                    if (handler == null)
                    {
                        continue;
                    }

                    object integrationEvent = JsonConvert.DeserializeObject(jsonPayload, subscription.EventType);
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
    }
}

using System;
using System.Collections.Generic;
using System.Linq;

namespace Core.ServiceBus
{
    /// <summary>
    ///     In memory subscriptions manager.
    /// </summary>
    internal sealed class EventBusSubscriptionsManager
    {
        private readonly Dictionary<string, List<SubscriptionInfo>> _subscriptions;

        public EventBusSubscriptionsManager()
        {
            _subscriptions = new Dictionary<string, List<SubscriptionInfo>>();
        }

        public bool HasSubscriptionsForEvent(string eventType)
        {
            return _subscriptions.ContainsKey(eventType);
        }

        public void AddSubscription<TEvent, TEventHandler>(string eventType)
            where TEvent : IIntegrationEvent
            where TEventHandler : IIntegrationEventHandler<TEvent>
        {
            Type handlerType = typeof(TEventHandler);
            if (!HasSubscriptionsForEvent(eventType))
            {
                _subscriptions.Add(eventType, new List<SubscriptionInfo>());
            }
            else if (_subscriptions[eventType].Any(t => t.HandlerType == handlerType))
            {
                throw new ArgumentException($"Handler Type { handlerType.Name } already registered for '{eventType}'");
            }
            _subscriptions[eventType].Add(new SubscriptionInfo(typeof(TEvent), handlerType));
        }

        public void RemoveSubscription<TEvent, TEventHandler>(string eventType)
            where TEvent : IIntegrationEvent
            where TEventHandler : IIntegrationEventHandler<TEvent>
        {
            if (!HasSubscriptionsForEvent(eventType))
            {
                return;
            }

            Type handlerType = typeof(TEventHandler);
            IEnumerable<SubscriptionInfo> subscriptionsToRemove = _subscriptions[eventType].Where(t => t.HandlerType == handlerType);
            foreach (SubscriptionInfo subscription in subscriptionsToRemove)
            {
                _subscriptions[eventType].Remove(subscription);
            }
            if (_subscriptions[eventType].Count == 0)
            {
                _subscriptions.Remove(eventType);
            }
        }

        public IEnumerable<SubscriptionInfo> GetSubscriptionsForEvent(string eventType)
        {
            return _subscriptions[eventType];
        }

        internal sealed class SubscriptionInfo
        {
            public SubscriptionInfo(Type eventType, Type handlerType)
            {
                EventType = eventType;
                HandlerType = handlerType;
            }

            public Type HandlerType { get; private set; }
            public Type EventType { get; private set; }
        }
    }
}

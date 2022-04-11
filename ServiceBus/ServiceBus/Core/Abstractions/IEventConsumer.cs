namespace Core.ServiceBus
{
    public interface IEventConsumer
    {
        void Subscribe<TEvent, TEventHandler>(string eventType) where TEvent : IIntegrationEvent where TEventHandler : IIntegrationEventHandler<TEvent>;
        void Unsubscribe<TEvent, TEventHandler>(string eventType) where TEvent : IIntegrationEvent where TEventHandler : IIntegrationEventHandler<TEvent>;
    }
}

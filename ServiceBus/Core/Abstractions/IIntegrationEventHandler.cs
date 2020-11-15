using System.Threading.Tasks;

namespace Core.ServiceBus
{
    public interface IIntegrationEventHandler<in TEvent> where TEvent : IIntegrationEvent
    {
        Task Handle(TEvent eventData);
    }
}

using System.Threading.Tasks;

namespace Core.ServiceBus
{
    public interface IEventProducer
    {
        Task Send(IntegrationEvent eventData);
    }
}

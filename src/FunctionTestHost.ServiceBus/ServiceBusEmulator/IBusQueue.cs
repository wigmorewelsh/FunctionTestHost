using System.Collections.Immutable;
using System.Threading.Tasks;
using Orleans.Runtime;

namespace FunctionTestHost.ServiceBusEmulator;

public interface IBusQueue : IAddressable
{
    Task Enqueue(Message message);
    Task<Message> Recieve();
    Task<ImmutableList<Message>> Recieve(int count);
    Task Confirm(int tag);
    Task Subscribe(IQueueSubscriber queueSubscriber); 
}
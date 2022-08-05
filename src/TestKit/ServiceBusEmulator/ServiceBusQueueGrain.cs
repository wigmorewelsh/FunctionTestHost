using System.Collections.Immutable;
using System.Threading.Tasks;

namespace TestKit.ServiceBusEmulator;

public class ServiceBusQueueGrain : ServiceBusQueueGrainBase, IServiceBusQueueGrain
{
    public Task Notification()
    {
        throw new System.NotImplementedException();
    }

    public Task Enqueue(Message message)
    {
        throw new System.NotImplementedException();
    }

    public Task<Message> Recieve()
    {
        throw new System.NotImplementedException();
    }

    public Task<ImmutableList<Message>> Recieve(int count)
    {
        throw new System.NotImplementedException();
    }

    public Task Confirm(int tag)
    {
        throw new System.NotImplementedException();
    }

    public Task Subscribe(IQueueSubscriber queueSubscriber)
    {
        throw new System.NotImplementedException();
    }
}
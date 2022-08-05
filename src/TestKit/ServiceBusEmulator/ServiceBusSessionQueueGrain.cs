using System.Collections.Immutable;
using System.Threading.Tasks;

namespace TestKit.ServiceBusEmulator;

public class ServiceBusSessionQueueGrain : ServiceBusQueueGrainBase, IServiceBusSessionQueueGrain
{
    private IQueueSubscriber? subscriber = null;

    public Task Notification()
    {
        throw new System.NotImplementedException();
    }

    public Task Enqueue(Message message)
    {
        _queue.Enqueue(message);
        return Task.CompletedTask;
    }

    public async Task<Message> Recieve()
    {
        return _queue.Peek();
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
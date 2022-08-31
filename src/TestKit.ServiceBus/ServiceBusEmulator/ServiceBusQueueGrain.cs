using System.Collections.Immutable;
using Azure.Messaging.ServiceBus;

namespace TestKit.ServiceBus.ServiceBusEmulator;

public class ServiceBusQueueGrain : ServiceBusQueueGrainBase, IServiceBusQueueGrain
{
    public Task Notification()
    {
        throw new System.NotImplementedException();
    }

    public Task Enqueue(ServiceBusMessage message)
    {
        _queue.Enqueue(message);
        return Task.CompletedTask;
    }

    public async Task<ServiceBusMessage> Recieve()
    {
        return _queue.Dequeue();
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
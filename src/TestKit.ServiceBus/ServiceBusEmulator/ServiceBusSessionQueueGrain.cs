using System.Collections.Immutable;
using Azure.Messaging.ServiceBus;

namespace TestKit.ServiceBus.ServiceBusEmulator;

public class ServiceBusSessionQueueGrain : ServiceBusQueueGrainBase, IServiceBusSessionQueueGrain
{
    private IQueueSubscriber? subscriber = null;

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
        return _queue.Peek();
    }

    public Task<ImmutableList<Message>> Recieve(int count)
    {
        throw new System.NotImplementedException();
    }

    public async Task Abandon(string messageId)
    {
        throw new NotImplementedException();
    }

    public Task Complete(string messageId)
    {
        throw new System.NotImplementedException();
    }

    public Task Subscribe(IQueueSubscriber queueSubscriber)
    {
        throw new System.NotImplementedException();
    }
}
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

    public Task Abandon(string messageId)
    {
        return Task.CompletedTask;
    }

    public Task Complete(string messageId)
    {
        return Task.CompletedTask;
    }

    public Task Subscribe(IQueueSubscriber queueSubscriber)
    {
        throw new System.NotImplementedException();
    }
}
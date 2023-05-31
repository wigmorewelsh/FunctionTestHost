using System.Collections.Immutable;
using Azure.Messaging.ServiceBus;
using AzureFunctionsRpcMessages;
using Google.Protobuf;
using TestKit.Actors;

namespace TestKit.ServiceBus.ServiceBusEmulator;

public class ServiceBusQueueGrain : ServiceBusQueueGrainBase, IServiceBusQueueGrain
{
    private List<(string loadRequestFunctionId, IFunctionInstanceGrain asReference)> _subscribers = new();

    public Task Notification()
    {
        throw new System.NotImplementedException();
    }

    public async Task Enqueue(ServiceBusMessage message)
    {
        _queue.Enqueue(message);
        if (_subscribers.Any())
        {
            var (functionId, asReference) = _subscribers.First();
            await asReference.Request(functionId, new TypedData()
            {
                Bytes = ByteString.CopyFrom(message.Body)
            });
            _queue.Dequeue();
        }
    }

    public async Task<ServiceBusMessage> Recieve()
    {
        return _queue.Dequeue();
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

    public Task Subscribe(string loadRequestFunctionId, IFunctionInstanceGrain asReference)
    {
        _subscribers.Add((loadRequestFunctionId, asReference));
        return Task.CompletedTask;
    }
}
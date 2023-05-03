using System.Collections.Immutable;
using Azure.Messaging.ServiceBus;
using Orleans.Runtime;

namespace TestKit.ServiceBus.ServiceBusEmulator;

public interface IBusQueue : IAddressable
{
    Task Enqueue(ServiceBusMessage message);
    Task<ServiceBusMessage> Recieve();
    Task<ImmutableList<Message>> Recieve(int count);
    Task Abandon(string messageId);
    Task Complete(string messageId);
    Task Subscribe(IQueueSubscriber queueSubscriber);
}
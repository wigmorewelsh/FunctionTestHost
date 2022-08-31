using System.Collections.Immutable;
using Azure.Messaging.ServiceBus;
using Orleans.Runtime;

namespace TestKit.ServiceBus.ServiceBusEmulator;

public interface IBusQueue : IAddressable
{
    Task Enqueue(ServiceBusMessage message);
    Task<ServiceBusMessage> Recieve();
    Task<ImmutableList<Message>> Recieve(int count);
    Task Confirm(int tag);
    Task Subscribe(IQueueSubscriber queueSubscriber); 
}
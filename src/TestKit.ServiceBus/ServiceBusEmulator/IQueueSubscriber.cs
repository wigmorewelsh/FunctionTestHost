using Orleans.Runtime;

namespace TestKit.ServiceBus.ServiceBusEmulator;

public interface IQueueSubscriber : IAddressable
{
    Task Notification();
}
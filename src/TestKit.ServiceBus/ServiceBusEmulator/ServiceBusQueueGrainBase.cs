using Azure.Messaging.ServiceBus;
using Orleans;

namespace TestKit.ServiceBus.ServiceBusEmulator;

public class ServiceBusQueueGrainBase : Grain
{
    protected Queue<ServiceBusMessage> _queue = new();
}
using Orleans;

namespace TestKit.ServiceBus.ServiceBusEmulator;

public interface IServiceBusQueueGrain : IGrainWithStringKey, IQueueSubscriber, IBusQueue
{
        
}
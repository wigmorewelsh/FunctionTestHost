using Orleans;

namespace TestKit.ServiceBus.ServiceBusEmulator;

public interface IServiceBusSessionQueueGrain : IGrainWithStringKey, IQueueSubscriber, IBusQueue
{
        
}
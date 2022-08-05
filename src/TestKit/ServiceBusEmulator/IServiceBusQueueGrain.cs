using Orleans;

namespace TestKit.ServiceBusEmulator;

public interface IServiceBusQueueGrain : IGrainWithStringKey, IQueueSubscriber, IBusQueue
{
        
}
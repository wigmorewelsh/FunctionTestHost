using Orleans;

namespace TestKit.ServiceBusEmulator;

public interface IServiceBusSessionQueueGrain : IGrainWithStringKey, IQueueSubscriber, IBusQueue
{
        
}
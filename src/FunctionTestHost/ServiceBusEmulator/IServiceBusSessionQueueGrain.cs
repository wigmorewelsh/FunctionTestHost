using Orleans;

namespace FunctionTestHost.ServiceBusEmulator
{
    public interface IServiceBusSessionQueueGrain : IGrainWithStringKey, IQueueSubscriber, IBusQueue
    {
        
    }
}
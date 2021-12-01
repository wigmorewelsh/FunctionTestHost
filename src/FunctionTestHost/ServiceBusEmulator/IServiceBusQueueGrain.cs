using Orleans;

namespace FunctionTestHost.ServiceBusEmulator
{
    public interface IServiceBusQueueGrain : IGrainWithStringKey, IQueueSubscriber, IBusQueue
    {
        
    }
}
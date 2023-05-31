using Orleans;
using TestKit.Actors;

namespace TestKit.ServiceBus.ServiceBusEmulator;

public interface IServiceBusQueueGrain : IGrainWithStringKey, IQueueSubscriber, IBusQueue
{
    Task Subscribe(string loadRequestFunctionId, IFunctionInstanceGrain asReference);
}
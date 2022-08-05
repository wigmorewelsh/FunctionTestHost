using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Orleans;
using TestKit.ServiceBus.ServiceBusEmulator;

namespace TestKit.ServiceBus;

public class TestKitServiceBusClient : ServiceBusClient 
{
    private readonly IGrainFactory _factory;

    public TestKitServiceBusClient(IGrainFactory factory)
    {
        _factory = factory;
    }

    public override ServiceBusReceiver CreateReceiver(string queueName)
    {
        return new TestKitReciever(_factory.GetGrain<IServiceBusQueueGrain>(queueName));
    }

    public override ServiceBusSender CreateSender(string queueOrTopicName)
    {
        return new TestKitSender(_factory.GetGrain<IServiceBusQueueGrain>(queueOrTopicName));
    }

    public override ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }
}
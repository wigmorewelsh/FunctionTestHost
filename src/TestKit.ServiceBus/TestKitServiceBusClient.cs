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
        return new TestKitReceiver(_factory.GetGrain<IServiceBusQueueGrain>(queueName));
    }

    public override ServiceBusReceiver CreateReceiver(string topicName, string subscriptionName)
    {
        return new TestKitSessionReceiver(_factory.GetGrain<IServiceBusSessionQueueGrain>(topicName + subscriptionName));
    }

    public override async Task<ServiceBusSessionReceiver> AcceptSessionAsync(string queueName, string sessionId, ServiceBusSessionReceiverOptions options = null, CancellationToken cancellationToken = default)
    {
        return new TestKitSessionReceiver(_factory.GetGrain<IServiceBusSessionQueueGrain>(queueName+sessionId));
    }

    public override ServiceBusSender CreateSender(string queueOrTopicName)
    {
        return new TestKitSender(_factory.GetGrain<IServiceBusQueueGrain>(queueOrTopicName), sessionId => _factory.GetGrain<IServiceBusSessionQueueGrain>(queueOrTopicName+sessionId));
    }

    public override ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }
}
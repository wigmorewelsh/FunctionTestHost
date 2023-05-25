using System;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using TestKit.ServiceBus.ServiceBusEmulator;

namespace TestKit.ServiceBus;

public class TestKitReceiver : ServiceBusReceiver
{
    private readonly IServiceBusQueueGrain _getGrain;

    public TestKitReceiver(IServiceBusQueueGrain getGrain)
    {
        _getGrain = getGrain;
    }

    public override async Task<ServiceBusReceivedMessage> ReceiveMessageAsync(TimeSpan? maxWaitTime = null, CancellationToken cancellationToken = new CancellationToken())
    {
        var next = await _getGrain.Recieve();

        return ToReceived(next);
    }

    public override async Task CompleteMessageAsync(ServiceBusReceivedMessage message,
        CancellationToken cancellationToken = new CancellationToken())
    {
        await _getGrain.Complete(message.MessageId);
    }

    public override async Task AbandonMessageAsync(ServiceBusReceivedMessage message, IDictionary<string, object> propertiesToModify = null,
        CancellationToken cancellationToken = new CancellationToken())
    {
        await _getGrain.Abandon(message.MessageId);
    }

    private ServiceBusReceivedMessage ToReceived(ServiceBusMessage message)
    {
        return ServiceBusModelFactory.ServiceBusReceivedMessage(
            body: message.Body,
            messageId: message.MessageId,
            partitionKey: message.PartitionKey,
            sessionId: message.SessionId);
    }
}
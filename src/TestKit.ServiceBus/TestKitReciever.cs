using System;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using TestKit.ServiceBus.ServiceBusEmulator;

namespace TestKit.ServiceBus;

public class TestKitReciever : ServiceBusReceiver
{
    private readonly IServiceBusQueueGrain _getGrain;

    public TestKitReciever(IServiceBusQueueGrain getGrain)
    {
        _getGrain = getGrain;
    }

    public override async Task<ServiceBusReceivedMessage> ReceiveMessageAsync(TimeSpan? maxWaitTime = null, CancellationToken cancellationToken = new CancellationToken())
    {
        var next = await _getGrain.Recieve();

        return ToReceived(next);
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
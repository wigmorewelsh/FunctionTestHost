using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using TestKit.ServiceBus.ServiceBusEmulator;

namespace TestKit.ServiceBus;

public class TestKitSender : ServiceBusSender
{
    private readonly IServiceBusQueueGrain _getGrain;

    public TestKitSender(IServiceBusQueueGrain getGrain)
    {
        _getGrain = getGrain;
    }

    public override async Task SendMessageAsync(ServiceBusMessage message, CancellationToken cancellationToken = new CancellationToken())
    {
        await _getGrain.Enqueue(message);
    }
}
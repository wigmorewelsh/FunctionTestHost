using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using TestKit.ServiceBus.ServiceBusEmulator;

namespace TestKit.ServiceBus;

public class TestKitSender : ServiceBusSender
{
    private readonly IServiceBusQueueGrain _getGrain;
    private readonly Func<string, IServiceBusSessionQueueGrain> _getSessionGrain;

    public TestKitSender(IServiceBusQueueGrain getGrain, Func<string, IServiceBusSessionQueueGrain> getSessionGrain)
    {
        _getGrain = getGrain;
        _getSessionGrain = getSessionGrain;
    }

    public override async Task SendMessageAsync(ServiceBusMessage message, CancellationToken cancellationToken = new CancellationToken())
    {
        if (message.SessionId is not null or "")
        {
           var sessionGrain = _getSessionGrain(message.SessionId);
           await sessionGrain.Enqueue(message);
           return;
        }

        await _getGrain.Enqueue(message);
    }
}
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Shouldly;
using Xunit;

namespace TestKit.ServiceBus;

public class Samples03 : IClassFixture<ServiceBusTestHost>
{
    private readonly ServiceBusTestHost _testHost;

    public Samples03(ServiceBusTestHost testHost)
    {
        _testHost = testHost;
    }

    [Fact]
    public async Task SendAndAbandonAMessage()
    {
        string queueName = "test";

        // since ServiceBusClient implements IAsyncDisposable we create it with "await using"
        await using var client = await _testHost.CreateServiceBusClient();

        // create the sender
        var sender = client.CreateSender(queueName);

        // create a message that we can send
        var message = new ServiceBusMessage("Hello world!")
        {
            SessionId = "mySessionId"
        };

        // send the message
        await sender.SendMessageAsync(message);

        // create a receiver that we can use to receive and settle the message
        var receiver = await client.AcceptSessionAsync(queueName, "mySessionId");

        // the received message is a different type as it contains some service set properties
        var receivedMessage = await receiver.ReceiveMessageAsync();

        // get the message body as a string
        var body = receivedMessage.Body.ToString();
        body.ShouldBe("Hello world!");
    }
}
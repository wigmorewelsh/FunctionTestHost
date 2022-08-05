using System.Threading.Tasks;
using Azure.Core.Amqp;
using Azure.Messaging.ServiceBus;
using Shouldly;
using Xunit;

namespace TestKit.ServiceBus;

public class Samples : IClassFixture<ServiceBusTestHost>
{
    private readonly ServiceBusTestHost _testHost;

    public Samples(ServiceBusTestHost testHost)
    {
        _testHost = testHost;
    }
    
    [Fact]
    public async Task SendAndReceiveMessage()
    {
        string queueName = "test";

        // since ServiceBusClient implements IAsyncDisposable we create it with "await using"
        await using var client = await _testHost.CreateServiceBusClient();

        // create the sender
        var sender = client.CreateSender(queueName);
        sender.ShouldBeOfType<TestKitSender>();

        // create a message that we can send. UTF-8 encoding is used when providing a string.
        var message = new ServiceBusMessage("Hello world!");

        // send the message
        await sender.SendMessageAsync(message);

        // create a receiver that we can use to receive the message
        ServiceBusReceiver receiver = client.CreateReceiver(queueName);

        // the received message is a different type as it contains some service set properties
        ServiceBusReceivedMessage receivedMessage = await receiver.ReceiveMessageAsync();

        // get the message body as a string
        string body = receivedMessage.Body.ToString();
        body.ShouldBe("Hello world!");

    } 
}
using System.Threading.Tasks;
using Azure.Core.Amqp;
using Azure.Messaging.ServiceBus;
using Shouldly;
using Xunit;

namespace TestKit.ServiceBus;

public class Samples01_02 : IClassFixture<ServiceBusTestHost>
{
    private readonly ServiceBusTestHost _testHost;

    public Samples01_02(ServiceBusTestHost testHost)
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

    [Fact]
    public async Task SendAndCompleteAMessage()
    {
        string queueName = "test";

        // since ServiceBusClient implements IAsyncDisposable we create it with "await using"
        await using var client = await _testHost.CreateServiceBusClient();

        // create the sender
        var sender = client.CreateSender(queueName);

        // create a message that we can send
        var message = new ServiceBusMessage("Hello world!");

        // send the message
        await sender.SendMessageAsync(message);

        // create a receiver that we can use to receive and settle the message
        var receiver = client.CreateReceiver(queueName);

        // the received message is a different type as it contains some service set properties
        var receivedMessage = await receiver.ReceiveMessageAsync();

        // complete the message, thereby deleting it from the service
        await receiver.CompleteMessageAsync(receivedMessage);
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
        var message = new ServiceBusMessage("Hello world!");

        // send the message
        await sender.SendMessageAsync(message);

        // create a receiver that we can use to receive and settle the message
        var receiver = client.CreateReceiver(queueName);

        // the received message is a different type as it contains some service set properties
        var receivedMessage = await receiver.ReceiveMessageAsync();

        // complete the message, thereby deleting it from the service
        await receiver.AbandonMessageAsync(receivedMessage);
    }
}
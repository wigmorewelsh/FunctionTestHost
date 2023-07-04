using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using NSubstitute;
using TestKit.ServiceBus;
using Xunit;

namespace TestKit.Tests;

public class ServiceBusTriggerTest : IClassFixture<FunctionTestCallbackHost>
{
    private readonly FunctionTestCallbackHost _testCallbackHost;

    public ServiceBusTriggerTest(FunctionTestCallbackHost testCallbackHost)
    {
        _testCallbackHost = testCallbackHost;
    }

    [Fact(Skip = "Service bus not being published")]
    public async Task CallSimpleFunction()
    {
        _testCallbackHost.ExecutionCallback.ClearReceivedCalls();
        await _testCallbackHost.CreateServer();

        await using var client = await _testCallbackHost.CreateServiceBusClient();
        var sender = client.CreateSender("somequeue");
        // create a message that we can send. UTF-8 encoding is used when providing a string.
        var message = new ServiceBusMessage("Hello world!");

        // send the message
        await sender.SendMessageAsync(message);
        await Task.Delay(10);
        _testCallbackHost.ExecutionCallback.Received().Called();
    }
}
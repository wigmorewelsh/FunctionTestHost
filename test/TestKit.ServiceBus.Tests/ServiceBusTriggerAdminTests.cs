using System.Text;
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

 

    [Fact]
    public async Task CallSimpleFunction()
    {
        _testCallbackHost.ExecutionCallback.ClearReceivedCalls();

        await using var client = await _testCallbackHost.CreateServiceBusClient();
        var sender = client.CreateSender("somequeue");
        // create a message that we can send. UTF-8 encoding is used when providing a string.
        var message = new ServiceBusMessage("Hello world!");

        // send the message
        await sender.SendMessageAsync(message);
        await Task.Delay(100);
        _testCallbackHost.ExecutionCallback.Received().Called();
    }
}

public class ServiceBusTriggerAdminTests : IClassFixture<FunctionTestCallbackHost>
{
    private readonly FunctionTestCallbackHost _testCallbackHost;

    public ServiceBusTriggerAdminTests(FunctionTestCallbackHost testCallbackHost)
    {
        _testCallbackHost = testCallbackHost;
    }

    // string
    // byte[]
    // json
    [Fact(Skip = "Not yet supported")]
    public async Task CallSimpleFunction_Errro()
    {
        _testCallbackHost.ExecutionCallback.ClearReceivedCalls();

        var response = await _testCallbackHost.CallFunction("admin/SimpleServiceBusCall");
        _testCallbackHost.ExecutionCallback.Received().Called();
    }

    [Fact]
    public async Task CallSimpleFunction()
    {
        _testCallbackHost.ExecutionCallback.ClearReceivedCalls();

        var response = await _testCallbackHost.CallFunction("admin/SimpleServiceBusCall", Encoding.UTF8.GetBytes("bar"));
        _testCallbackHost.ExecutionCallback.Received().Called();
    }

    [Fact]
    public async Task CallBatchFunction()
    {
        _testCallbackHost.ExecutionCallback.ClearReceivedCalls();

        var response = await _testCallbackHost.CallFunction("admin/BatchServiceBusCall", Encoding.UTF8.GetBytes("bar"));
        _testCallbackHost.ExecutionCallback.Received().Called();
    }
}
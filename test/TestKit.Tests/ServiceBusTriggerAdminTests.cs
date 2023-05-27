using System.Text;
using System.Threading.Tasks;
using NSubstitute;
using Xunit;

namespace TestKit.Tests;

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
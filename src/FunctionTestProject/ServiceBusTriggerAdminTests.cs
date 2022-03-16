using System.Net.Http.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FunctionAppOne;
using FunctionTestHost.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NSubstitute;
using Shouldly;
using Xunit;

namespace FunctionTestProject;

public class FunctionTestHostCallback : FunctionTestHost<Program>
{
    public IThings Thing { get; private set; }

    public override void ConfigureFunction(IHostBuilder host)
    {
        Thing = Substitute.For<IThings>();
        host.ConfigureServices(services =>
        {
            services.AddSingleton(Thing);
        });
    }
}

public class ServiceBusTriggerAdminTests : IClassFixture<FunctionTestHostCallback>
{
    private readonly FunctionTestHostCallback _testHost;

    public ServiceBusTriggerAdminTests(FunctionTestHostCallback testHost)
    {
        _testHost = testHost;
    }

    // string
    // byte[]
    // json
    [Fact(Skip = "Not yet supported")]
    public async Task CallSimpleFunction_Errro()
    {
        _testHost.Thing.ClearReceivedCalls();

        var response = await _testHost.CallFunction("admin/SimpleServiceBusCall");
        _testHost.Thing.Received().Called();
    }

    [Fact]
    public async Task CallSimpleFunction()
    {
        _testHost.Thing.ClearReceivedCalls();

        var response = await _testHost.CallFunction("admin/SimpleServiceBusCall", Encoding.UTF8.GetBytes("bar"));
        _testHost.Thing.Received().Called();
    }
}
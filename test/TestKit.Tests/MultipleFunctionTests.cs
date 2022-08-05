using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using TestKit.TestHost;
using Xunit;

namespace TestKit.Tests;

public class MultipleFunctionHost : FunctionTestHost
{
    protected override void ConfigureTestHost(IFunctionTestHostBuilder builder)
    {
        builder.AddFunction<FunctionAppOne.Program>()
             .WithServiceConfiguration((IHostBuilder builder) => { });
        builder.AddFunction<FunctionAppTwo.Program>();
    }
}

public class MultipleFunctionTests : IClassFixture<MultipleFunctionHost>
{
    private readonly MultipleFunctionHost _host;

    public MultipleFunctionTests(MultipleFunctionHost host)
    {
        _host = host;
    }
    
    [Fact]
    public async Task SomeTest()
    {
        await _host.CallFunction("SampleFunctionOne/Hello");
        await _host.CallFunction("SampleFunctionOne/Hello");
    }
}
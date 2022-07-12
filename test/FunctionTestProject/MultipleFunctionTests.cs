using System.Threading.Tasks;
using FunctionTestHost.TestHost;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace FunctionTestProject;

public class MultipleFunctionHost : FunctionTestHost.TestHost.FunctionTestHost
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
        await _host.CallFunction("FunctionAppOne/Hello");
        await _host.CallFunction("FunctionAppTwo/Hello");
    }
}
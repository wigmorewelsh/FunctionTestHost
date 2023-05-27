using FunctionAppOne;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NSubstitute;
using TestKit.Actors;
using TestKit.TestHost;

namespace TestKit.Tests;

public class FunctionTestCallbackHost : FunctionTestHost<Program>
{
    public IExecutionCallback ExecutionCallback { get; private set; }

    public override void ConfigureFunction(IHostBuilder host)
    {
        ExecutionCallback = Substitute.For<IExecutionCallback>();
        host.ConfigureServices(services =>
        {
            services.AddSingleton(ExecutionCallback);
        });
        
    }
}
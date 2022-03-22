using FunctionAppOne;
using FunctionTestHost.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NSubstitute;

namespace FunctionTestProject;

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
using System;
using Microsoft.Extensions.Hosting;

namespace TestKit.TestHost;

public interface IConfigureFunctionTestHost
{
    (int, int) HostPorts { get; }
}

public class NullConfigureFunctionTestHost : IConfigureFunctionTestHost
{
    public NullConfigureFunctionTestHost((int, int) hostPorts)
    {
        HostPorts = hostPorts;
    }

    public (int, int) HostPorts { get; }
}

public class FunctionTestHost<TStartup> : FunctionTestHost, IConfigureFunctionTestHost
{
    private FunctionTestApp<TStartup> _functionTestApp;

    public FunctionTestHost()
    {
        _functionTestApp = new FunctionTestApp<TStartup>(this);
        _functionTestApp.WithServiceConfiguration(ConfigureFunction);
        AddFunction(_functionTestApp);
    }

    public virtual void ConfigureFunction(IHostBuilder host) { }
    
    public IServiceProvider Services => _functionTestApp.Services;
}
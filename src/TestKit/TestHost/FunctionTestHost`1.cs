using System;
using Microsoft.Extensions.DependencyInjection;
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

/// <summary>
/// The `FunctionTestHost<T>` is a simplified version of the `FunctionTestHost` that assumes you have a single function app under test.
/// </summary>
/// <typeparam name="TStartup"></typeparam>
public class FunctionTestHost<TStartup> : FunctionTestHost, IConfigureFunctionTestHost
{
    private FunctionTestApp<TStartup> _functionTestApp;

    public FunctionTestHost()
    {
        _functionTestApp = new FunctionTestApp<TStartup>(this);
        _functionTestApp.WithServiceConfiguration(ConfigureFunction);
        AddFunction(_functionTestApp);
        base.ConfigureHostExtensions(ConfigureExtensions);
    }

    public virtual void ConfigureFunction(IHostBuilder host) { }
    public virtual void ConfigureExtensions(IServiceCollection serviceCollection) { }
    
    public IServiceProvider Services => _functionTestApp.Services;
}
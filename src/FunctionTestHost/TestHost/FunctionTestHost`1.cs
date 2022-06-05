using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using AzureFunctionsRpcMessages;
using FunctionTestHost.Actors;
using FunctionTestHost.Utils;
using Google.Protobuf;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Orleans;
using Xunit;

namespace FunctionTestHost.TestHost;

public class FunctionTestHost<TStartup> : FunctionTestHost
{
    private FunctionTestApp<TStartup> _functionTestApp;

    public FunctionTestHost()
    {
        _functionTestApp = new FunctionTestApp<TStartup>(this);
        AddFunction(_functionTestApp);
    }

    public virtual void ConfigureFunction(IHostBuilder host) { }
    
    public IServiceProvider Services => _functionTestApp.Services;
}
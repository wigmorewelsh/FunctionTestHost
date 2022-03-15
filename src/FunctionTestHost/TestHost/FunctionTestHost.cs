using System;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using FunctionTestHost;
using FunctionTestHost.Actors;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using Xunit;

namespace FunctionTestProject;

public class FunctionTestHost<TStartup> : IAsyncDisposable, IAsyncLifetime
{
    private AsyncLock _lock = new();
    private volatile bool _isInit = false;
    private volatile bool _isDisposed = false;

    private IHost _fakeHost;
    private FunctionTestApp<TStartup> _functionHost;

    public async Task CreateServer()
    {
        if(_isInit) return;
        using var _ = await _lock.LockAsync();
        if(_isInit) return;

        _fakeHost = Host.CreateDefaultBuilder()
            .UseOrleans(orleans =>
            {
                orleans.Configure<SerializationProviderOptions>(opt =>
                {
                    opt.SerializationProviders.Add(typeof(ProtobufNetSerializer));
                });
                orleans.UseLocalhostClustering();
                orleans.ConfigureApplicationParts(parts =>
                    parts.AddApplicationPart(typeof(FunctionInstanceGrain).Assembly));
            })
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.ConfigureKestrel(k =>
                    k.ListenLocalhost(WorkerConfig.Port, opt => opt.Protocols = HttpProtocols.Http2)).UseStartup<Startup>();
            })
            .Build();

        _fakeHost.Start();

        _functionHost = new FunctionTestApp<TStartup>(this);
        await _functionHost.Start();


        _isInit = true;
    }

    public async Task InitializeAsync()
    {
        await CreateServer();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await ((IAsyncDisposable)this).DisposeAsync();
    }

    async ValueTask IAsyncDisposable.DisposeAsync()
    {
        if(_isDisposed) return;
        using var _ = await _lock.LockAsync();
        if(_isDisposed) return;

        await _functionHost.DisposeAsync();
        await _fakeHost.StopAsync(TimeSpan.FromMilliseconds(0));
        _isDisposed = true;

    }

    public async Task<string> CallFunction(string functionName)
    {
        await CreateServer();
        var factory = _fakeHost.Services.GetRequiredService<IGrainFactory>();
        var funcGrain = factory.GetGrain<IFunctionEndpointGrain>(functionName);
        var response = await funcGrain.Call();
        if (response.ReturnValue.Http is { } http)
        {
            if (http.Body.Bytes is { } bytes)
            {
                return Encoding.UTF8.GetString(Convert.FromBase64String(bytes.ToBase64()));
            }
        }
        return response.Result.Result;
    }

    public async Task<string> CallFunction(string functionName, JsonContent body)
    {
        await CreateServer();
        var factory = _fakeHost.Services.GetRequiredService<IGrainFactory>();
        var funcGrain = factory.GetGrain<IFunctionEndpointGrain>(functionName);

        var response = await funcGrain.Call();
        if (response.ReturnValue.Http is { } http)
        {
            if (http.Body.Bytes is { } bytes)
            {
                return Encoding.UTF8.GetString(Convert.FromBase64String(bytes.ToBase64()));
            }
        }
        return response.Result.Result;
    }

    public virtual void ConfigureFunction(IHostBuilder host)
    {
    }
}
using System;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using AzureFunctionsRpcMessages;
using FunctionTestHost.Actors;
using FunctionTestHost.Utils;
using Google.Protobuf;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using Xunit;

namespace FunctionTestHost.TestHost;

public class FunctionTestHost<TStartup> : IAsyncDisposable, IAsyncLifetime
{
    public FunctionTestHost()
    {
        HostPorts = TestClusterPortAllocator.Instance.AllocateConsecutivePortPairs(2);
    }

    private AsyncLock _lock = new();
    private volatile bool _isInit = false;
    private volatile bool _isDisposed = false;

    private IHost _fakeHost;
    private FunctionTestApp<TStartup> _functionHost;
    public (int, int) HostPorts { get; }

    public async Task CreateServer()
    {
        if(_isInit) return;
        using var _ = await _lock.LockAsync();
        if(_isInit) return;

        var ports = TestClusterPortAllocator.Instance.AllocateConsecutivePortPairs(2);

        _fakeHost = Host.CreateDefaultBuilder()
            .UseOrleans(orleans =>
            {
                orleans.Configure<SerializationProviderOptions>(opt =>
                {
                    opt.SerializationProviders.Add(typeof(ProtobufNetSerializer));
                });
                orleans.UseLocalhostClustering(ports.Item1, ports.Item2);
                orleans.ConfigureApplicationParts(parts =>
                    parts.AddApplicationPart(typeof(FunctionInstanceGrain).Assembly));
            })
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.ConfigureKestrel(k =>
                    k.ListenLocalhost(HostPorts.Item1, opt => opt.Protocols = HttpProtocols.Http2)).UseStartup<Startup>();
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
        var funcGrain = await GetEndpointGrain(functionName);
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
        var funcGrain = await GetEndpointGrain(functionName);

        var httpBody = new RpcHttp();
        httpBody.Body = new TypedData
        {
            Bytes = ByteString.FromStream(await body.ReadAsStreamAsync())
        };
        var response = await funcGrain.Call(httpBody);
        if (response.ReturnValue.Http is { } http)
        {
            if (http.Body.Bytes is { } bytes)
            {
                return Encoding.UTF8.GetString(Convert.FromBase64String(bytes.ToBase64()));
            }
        }
        return response.Result.Result;
    }

    private async Task<IPublicEndpoint> GetEndpointGrain(string functionName)
    {
        await CreateServer();
        var factory = _fakeHost.Services.GetRequiredService<IGrainFactory>();
        if (functionName.StartsWith("admin"))
        {
            return factory.GetGrain<IFunctionAdminEndpointGrain>(functionName);
        }

        return factory.GetGrain<IFunctionEndpointGrain>(functionName);
    }

    public virtual void ConfigureFunction(IHostBuilder host)
    {
    }

    public async Task<string> CallFunction(string functionName, byte[] getBytes)
    {
        var funcGrain = await GetEndpointGrain(functionName);
        var httpBody = new RpcHttp();
        httpBody.Body = new TypedData
        {
            Bytes = ByteString.CopyFrom(getBytes)
        };
        var response = await funcGrain.Call(httpBody);
        if (response.ReturnValue?.Http is { } http)
        {
            if (http.Body.Bytes is { } bytes)
            {
                return Encoding.UTF8.GetString(Convert.FromBase64String(bytes.ToBase64()));
            }
        }

        if (response.Result?.Exception is { } err)
        {
            return err.Message;
        }
        return response.Result.Result;
    }
}
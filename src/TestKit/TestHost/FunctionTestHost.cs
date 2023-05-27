using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using AzureFunctionsRpcMessages;
using Google.Protobuf;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using TestKit.Actors;
using TestKit.Utils;
using Xunit;

namespace TestKit.TestHost;

public interface IFunctionTestHostBuilder
{
    ITestHostBuilder AddFunction<T>();
    void ConfigureHostExtensions(Action<IServiceCollection> serviceCollection);
}

public class FunctionTestHost : IFunctionTestHostBuilder, IAsyncDisposable, IAsyncLifetime
{
    private protected IHost _fakeHost;
    private protected List<FunctionTestApp> _functionHosts = new();
    private protected List<Action<IServiceCollection>> _hostExtensions = new();
    private protected volatile bool _isDisposed;
    private protected volatile bool _isInit;
    private protected AsyncLock _lock = new();
    private List<Action<ISiloBuilder>> _hostConfigs = new();
    public (int, int) HostPorts { get; protected set; }

    public FunctionTestHost()
    {
        HostPorts = TestClusterPortAllocator.Instance.AllocateConsecutivePortPairs(2);
    }

    async ValueTask IAsyncDisposable.DisposeAsync()
    {
        if (_isDisposed) return;
        using var _ = await _lock.LockAsync();
        if (_isDisposed) return;

        foreach (var functionHost in _functionHosts) await functionHost.DisposeAsync();
        await _fakeHost.StopAsync(TimeSpan.FromMilliseconds(0));
        _isDisposed = true;
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await ((IAsyncDisposable)this).DisposeAsync();
    }

    public async Task InitializeAsync()
    {
        await CreateServer();
    }

    public ITestHostBuilder AddFunction<T>()
    {
        var functionTestApp = new FunctionTestApp<T>(new NullConfigureFunctionTestHost(HostPorts));
        AddFunction(functionTestApp);
        return functionTestApp;
    }



    private protected async Task StartHost((int, int) ports)
    {
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
                foreach (Action<ISiloBuilder> hostConfig in _hostConfigs)
                {
                    hostConfig.Invoke(orleans);
                }
            })
            .ConfigureWebHostDefaults(webBuilder =>
            {
                var builder = webBuilder.ConfigureKestrel(k =>
                        k.ListenLocalhost(HostPorts.Item1, opt => opt.Protocols = HttpProtocols.Http2))
                    .UseStartup<Startup>();
                foreach (var extension in _hostExtensions)
                {
                    builder.ConfigureServices(extension);
                }
                
            })
            .Build();

        await _fakeHost.StartAsync();
    }

    protected virtual void ConfigureTestHost(IFunctionTestHostBuilder builder) { }

    protected void AddFunction(FunctionTestApp functionTestApp)
    {
        _functionHosts.Add(functionTestApp);
    }

    public void ConfigureHostExtensions(Action<IServiceCollection> serviceCollection)
    {
        _hostExtensions.Add(serviceCollection);
    }
    
    private protected async Task StartFunctions()
    {
        foreach (var functionHost in _functionHosts) await functionHost.Start();
    }

    public async Task CreateServer()
    {
        if (_isInit) return;
        using var _ = await _lock.LockAsync();
        if (_isInit) return;

        var ports = TestClusterPortAllocator.Instance.AllocateConsecutivePortPairs(2);

        await StartHost(ports);

        ConfigureTestHost(this);        

        await StartFunctions();

        _isInit = true;
    }

    private async Task<IPublicEndpoint> GetEndpointGrain(string functionName)
    {
        var serviceProvider = await CreateHostServiceProvider();
        var factory = serviceProvider.GetRequiredService<IGrainFactory>();
        if (functionName.StartsWith("admin")) return factory.GetGrain<IFunctionAdminEndpointGrain>(functionName);


        return factory.GetGrain<IFunctionEndpointGrain>(functionName);
    }

    public async Task<IServiceProvider> CreateHostServiceProvider()
    {
        await CreateServer();
        return _fakeHost.Services;
    }

    public async Task<string> CallFunction(string functionName, byte[]? getBytes)
    {
        var funcGrain = await GetEndpointGrain(functionName);
        var httpBody = new RpcHttp();
        if (getBytes != null)
            httpBody.Body = new TypedData
            {
                Bytes = ByteString.CopyFrom(getBytes)
            };

        var response = await funcGrain.Call(httpBody);
        if (response.ReturnValue?.Http is { } http)
            if (http.Body.Bytes is { } bytes)
                return Encoding.UTF8.GetString(Convert.FromBase64String(bytes.ToBase64()));

        if (response.Result?.Exception is { } err) return err.Message;
        return response.Result.Result;
    }

    public async Task<string> CallFunction(string functionName)
    {
        return await CallFunction(functionName, (byte[]?)null);
    }

    public async Task<string> CallFunction(string functionName, JsonContent body)
    {
        return await CallFunction(functionName, await body.ReadAsByteArrayAsync());
    }

    public void ConfigureHost(Action<ISiloBuilder> func)
    {
       _hostConfigs.Add(func); 
    }
}
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using FunctionMetadataEndpoint;
using Grpc.Net.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using TestKit.MetadataClient;
using TestKit.Utils;

namespace TestKit.TestHost;

public class FunctionTestApp<TStartup> : FunctionTestApp, ITestHostBuilder
{
    private readonly IConfigureFunctionTestHost _functionTestHost;
    private AsyncLock _lock = new();
    private volatile bool _isInit = false;
    private IHost _functionHost;
    private Action<IHostBuilder>? _serviceConfiguration;

    public FunctionTestApp(IConfigureFunctionTestHost functionTestHost)
    {
        Environment.SetEnvironmentVariable("FUNCTIONS_APPLICATION_DIRECTORY", Environment.CurrentDirectory);
        Environment.SetEnvironmentVariable("FUNCTIONS_WORKER_DIRECTORY", Environment.CurrentDirectory);
        _functionTestHost = functionTestHost;
    }

    public async Task Start()
    {
        if(_isInit) return;
        using var _ = await _lock.LockAsync();
        if(_isInit) return;

        var builder = HostFactoryResolver.ResolveHostBuilderFactory<IHostBuilder>(typeof(TStartup).Assembly);
        var configureServices = builder(Array.Empty<string>())
            .ConfigureAppConfiguration(config =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["Host"] = "localhost",
                    ["Port"] = _functionTestHost.HostPorts.Item1.ToString(),
                    ["WorkerId"] = Guid.NewGuid().ToString(),
                    ["GrpcMaxMessageLength"] = (2_147_483_647).ToString()
                });
            })
            .ConfigureServices((host, services) =>
            {
                services.Configure<GrpcWorkerStartupOptions>(host.Configuration);
                services.AddSingleton(ctx =>
                {
                    var options = ctx.GetRequiredService<IOptions<GrpcWorkerStartupOptions>>();
                    var url = new Uri($"http://{options.Value.Host}:{options.Value.Port}");
                    var channel = GrpcChannel.ForAddress(url, new GrpcChannelOptions());
                    return new FunctionRpc.FunctionRpcClient(channel);
                });
                services.AddHostedService<MetadataClientRpc<TStartup>>();
            });
        _serviceConfiguration?.Invoke(configureServices);
        _functionHost = configureServices
            .Build();

        await _functionHost.StartAsync();
        _isInit = true;
    }

    public IServiceProvider Services => _functionHost.Services;

    public async ValueTask DisposeAsync()
    {
        await _functionHost.StopAsync(TimeSpan.Zero);
    }

    public ITestHostBuilder WithServiceConfiguration(Action<IHostBuilder> action)
    {
        _serviceConfiguration = action;
        return this;
    }
}
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FunctionMetadataEndpoint;
using FunctionTestHost.MetadataClient;
using FunctionTestProject.Utils;
using Grpc.Net.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace FunctionTestProject
{
    public class FunctionTestApp<TStartup> : IAsyncDisposable
    {
        private AsyncLock _lock = new();
        private volatile bool _isInit = false;
        private IHost _functionHost;

        public FunctionTestApp()
        {

        }

        public async Task Start()
        {
            if(_isInit) return;
            using var _ = await _lock.LockAsync();
            if(_isInit) return;

            var builder = HostFactoryResolver.ResolveHostBuilderFactory<IHostBuilder>(typeof(TStartup).Assembly);
            _functionHost = builder(Array.Empty<string>())
                .ConfigureAppConfiguration(config =>
                {
                    config.AddInMemoryCollection(new Dictionary<string, string>
                    {
                        ["Host"] = "localhost",
                        ["Port"] = WorkerConfig.Port.ToString(),
                        ["WorkerId"] = WorkerConfig.WorkerId,
                        ["GrpcMaxMessageLength"] = "1024"
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
                })
                .Build();

            _functionHost.Start();
            _isInit = true;
        }

        public async ValueTask DisposeAsync()
        {
            await _functionHost.StopAsync();
        }
    }
}
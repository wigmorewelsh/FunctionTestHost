using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FunctionAppOne;
using FunctionMetadataEndpoint;
using FunctionTestHost;
using FunctionTestHost.Actors;
using FunctionTestHost.MetadataClient;
using FunctionTestProject.Utils;
using Grpc.Net.Client;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Orleans;
using Orleans.Hosting;
using Xunit;

namespace FunctionTestProject
{
    public class FunctionTestHost<TStartup> : IAsyncDisposable, IAsyncLifetime
    {
        private AsyncLock _lock = new();
        private volatile bool _isInit = false;

        private IHost _fakeHost;
        private IHost _functionHost;
        private const int Port = 20222;
        private const string WorkerId = "123";

        public async Task CreateServer()
        {
            if(_isInit) return;
            using var _ = await _lock.LockAsync();
            if(_isInit) return;

            _fakeHost = Host.CreateDefaultBuilder()
                .UseOrleans(orleans =>
                {
                    orleans.UseLocalhostClustering();
                    orleans.ConfigureApplicationParts(parts =>
                        parts.AddApplicationPart(typeof(FunctionGrain).Assembly));
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.ConfigureKestrel(k =>
                        k.ListenLocalhost(Port, opt => opt.Protocols = HttpProtocols.Http2)).UseStartup<Startup>();
                })
                .Build();

            _fakeHost.Start();

            var builder = HostFactoryResolver.ResolveHostBuilderFactory<IHostBuilder>(typeof(TStartup).Assembly);
            _functionHost = builder(Array.Empty<string>())
                .ConfigureAppConfiguration(config =>
                {
                    config.AddInMemoryCollection(new Dictionary<string, string>
                    {
                        ["Host"] = "localhost",
                        ["Port"] = Port.ToString(),
                        ["WorkerId"] = WorkerId,
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
                    services.AddHostedService<MetadataClientRpc<Hello>>();
                })
                .Build();

            _functionHost.Start();
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
            await _functionHost.StopAsync();
            await _fakeHost.StopAsync();
        }
    }
}
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FunctionAppOne;
using FunctionMetadataEndpoint;
using FunctionTestHost;
using FunctionTestHost.MetadataClient;
using Grpc.Core;
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
    public class UnitTest1
    {
        [Fact]
        public async Task Test1()
        {
            using var fakeHost = Host.CreateDefaultBuilder()
                .UseOrleans(orleans =>
                {
                    orleans.UseLocalhostClustering();
                    orleans.ConfigureApplicationParts(parts => parts.AddFromDependencyContext());
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.ConfigureKestrel(k =>
                        k.ListenLocalhost(20222, opt => opt.Protocols = HttpProtocols.Http2)).UseStartup<Startup>();
                })
                .Build();

            fakeHost.Start();

            using var functionHost = new HostBuilder()
                .ConfigureAppConfiguration(config =>
                {
                    config.AddInMemoryCollection(new Dictionary<string, string>
                    {
                        ["Host"] = "localhost",
                        ["Port"] = "20222",
                        ["WorkerId"] = "123",
                        ["GrpcMaxMessageLength"] = "1024"
                    });
                })
                .ConfigureFunctionsWorkerDefaults()
                .ConfigureServices((host, services ) =>
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

            functionHost.Start();

            await Task.Delay(30_000);

            await functionHost.StopAsync();
            await fakeHost.StopAsync();
        }
    }
}
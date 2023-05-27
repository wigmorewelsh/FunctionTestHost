using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Orleans;
using Orleans.Hosting;
using TestKit.Actors;
using TestKit.ServiceBus.ServiceBusEmulator;
using TestKit.TestHost;

namespace TestKit.ServiceBus;

public static class FunctionTestHostExtensions
{
    public static void AddServiceBusTestHost(this FunctionTestHost testHost)
    {
        testHost.ConfigureHost(orleans =>
        {
            orleans.ConfigureApplicationParts(parts => parts.AddApplicationPart(typeof(ServiceBusQueueGrain).Assembly));
        });
    }

    public static void AddServiceBusExtension(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddSingleton<IDataMapperFactory, ServiceBusDataMapperFactory>();
    }

    public static async Task<TestKitServiceBusClient> CreateServiceBusClient(this FunctionTestHost testHost)
    {
        var svc = await testHost.CreateHostServiceProvider();
        var factory = svc.GetRequiredService<IGrainFactory>();
        return new TestKitServiceBusClient(factory);
    } 
}
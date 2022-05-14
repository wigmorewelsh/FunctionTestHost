using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FunctionMetadataEndpoint;
using FunctionTestHost.MetadataClient;
using FunctionTestHost.Utils;
using Grpc.Net.Client;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
// using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyModel;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace FunctionTestHost.TestHost;

internal class NoopHostLifetime : IHostLifetime
{
    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task WaitForStartAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}

public class FunctionTestApp<TStartup> : IAsyncDisposable
{
    private readonly FunctionTestHost<TStartup> _functionTestHost;
    private AsyncLock _lock = new();
    private volatile bool _isInit = false;
    private IHost _functionHost;

    public FunctionTestApp(FunctionTestHost<TStartup> functionTestHost)
    {
        _functionTestHost = functionTestHost;
    }

    public async Task Start()
    {
        if (_isInit) return;
        using var _ = await _lock.LockAsync();
        if (_isInit) return;

        var builder = HostFactoryResolver.ResolveHostBuilderFactory<IHostBuilder>(typeof(TStartup).Assembly);
        var hostBuilder = builder(Array.Empty<string>());
        var configureServices = hostBuilder
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
                services.AddSingleton<IHostLifetime, NoopHostLifetime>();
                // services.AddSingleton<IServer, TestServer>();
            });
        SetContentRoot(hostBuilder);
        this._functionTestHost.ConfigureFunction(configureServices);
        _functionHost = configureServices
            .Build();

        await _functionHost.StartAsync();
        _isInit = true;
    }

    private void SetContentRoot(IHostBuilder builder)
    {
        if (SetContentRootFromSetting(builder))
        {
            return;
        }

        var fromFile = File.Exists("FunctionTestingAppManifest.json");
        var contentRoot =
            fromFile
                ? GetContentRootFromFile("FunctionTestingAppManifest.json")
                : null; // GetContentRootFromAssembly();

        if (contentRoot != null)
        {
            builder.UseContentRoot(contentRoot);
        }
        else
        {
            // builder.UseSolutionRelativeContentRoot(typeof(TStartup).Assembly.GetName().Name);
        }
    }

    private string GetContentRootFromFile(string file)
    {
        var data = JsonSerializer.Deserialize<IDictionary<string, string>>(File.ReadAllBytes(file));
        var key = typeof(TStartup).Assembly.GetName().FullName;
        try
        {
            return data[key];
        }
        catch
        {
            throw new KeyNotFoundException(
                $"Could not find content root for project '{key}' in test manifest file '{file}'");
        }
    }

    private static bool SetContentRootFromSetting(IHostBuilder builder)
    {
        // Attempt to look for TEST_CONTENTROOT_APPNAME in settings. This should result in looking for
        // ASPNETCORE_TEST_CONTENTROOT_APPNAME environment variable.
        var assemblyName = typeof(TStartup).Assembly.GetName().Name;
        var settingSuffix = assemblyName.ToUpperInvariant().Replace(".", "_");
        var settingName = $"TEST_CONTENTROOT_{settingSuffix}";

        // var settingValue = builder.GetSetting(settingName);
        // if (settingValue == null)
        // {
        return false;
        // }

        // builder.UseContentRoot(settingValue);
        // return true;
    }

    /// <summary>
    /// Gets the assemblies containing the functional tests. The
    /// <see cref="WebApplicationFactoryContentRootAttribute"/> applied to these
    /// assemblies defines the content root to use for the given
    /// <typeparamref name="TEntryPoint"/>.
    /// </summary>
    /// <returns>The list of <see cref="Assembly"/> containing tests.</returns>
    protected virtual IEnumerable<Assembly> GetTestAssemblies()
    {
        try
        {
            // The default dependency context will be populated in .net core applications.
            var context = DependencyContext.Default;
            if (context == null || context.CompileLibraries.Count == 0)
            {
                // The app domain friendly name will be populated in full framework.
                return new[] { Assembly.Load(AppDomain.CurrentDomain.FriendlyName) };
            }

            var runtimeProjectLibraries = context.RuntimeLibraries
                .ToDictionary(r => r.Name, r => r, StringComparer.Ordinal);

            // Find the list of projects
            var projects = context.CompileLibraries.Where(l => l.Type == "project");

            var entryPointAssemblyName = typeof(TStartup).Assembly.GetName().Name;

            // Find the list of projects referencing TEntryPoint.
            var candidates = context.CompileLibraries
                .Where(library =>
                    library.Dependencies.Any(d =>
                        string.Equals(d.Name, entryPointAssemblyName, StringComparison.Ordinal)));

            var testAssemblies = new List<Assembly>();
            foreach (var candidate in candidates)
            {
                if (runtimeProjectLibraries.TryGetValue(candidate.Name, out var runtimeLibrary))
                {
                    var runtimeAssemblies = runtimeLibrary.GetDefaultAssemblyNames(context);
                    testAssemblies.AddRange(runtimeAssemblies.Select(Assembly.Load));
                }
            }

            return testAssemblies;
        }
        catch (Exception)
        {
        }

        return Array.Empty<Assembly>();
    }


    public async ValueTask DisposeAsync()
    {
        await _functionHost.StopAsync(TimeSpan.Zero);
    }
}
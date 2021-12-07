using System;
using System.Threading.Tasks;
using FunctionTestHost;
using FunctionTestHost.Actors;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Hosting;
using Orleans;
using Orleans.Hosting;
using Xunit;

namespace FunctionTestProject
{
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
                    orleans.UseLocalhostClustering();
                    orleans.ConfigureApplicationParts(parts =>
                        parts.AddApplicationPart(typeof(FunctionGrain).Assembly));
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.ConfigureKestrel(k =>
                        k.ListenLocalhost(WorkerConfig.Port, opt => opt.Protocols = HttpProtocols.Http2)).UseStartup<Startup>();
                })
                .Build();

            _fakeHost.Start();

            _functionHost = new FunctionTestApp<TStartup>();
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
    }
}
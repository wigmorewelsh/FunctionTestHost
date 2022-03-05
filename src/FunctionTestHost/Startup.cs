using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FunctionTestHost.Actors;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Orleans.Runtime;

namespace FunctionTestHost;

public class Startup
{
    // This method gets called by the runtime. Use this method to add services to the container.
    // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<LocalGrainActivator>();
        services.AddSingleton<IGrainActivator>(ctx => ctx.GetRequiredService<LocalGrainActivator>());
        services.AddSingleton<ILocalGrainCatalog>(ctx => ctx.GetRequiredService<LocalGrainActivator>());
        services.AddSingleton<ConnectionManager>();
        services.AddGrpc();
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseRouting();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapGrpcService<FunctionRpcService>();
            endpoints.MapGrpcService<FunctionMetadataService>();
            endpoints.Map("/api/function", async context =>
            {
            });

            endpoints.MapGet("/",
                async context =>
                {
                    await context.Response.WriteAsync(
                        "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");
                });
        });
    }
}
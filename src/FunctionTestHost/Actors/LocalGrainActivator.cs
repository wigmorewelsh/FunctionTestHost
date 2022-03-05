using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Orleans;
using Orleans.Core;
using Orleans.Runtime;

namespace FunctionTestHost.Actors;

public class LocalGrainActivator : DefaultGrainActivator, ILocalGrainCatalog
{
    private ConcurrentDictionary<IGrainIdentity, FunctionGrainContext> localGrains = new();

    public LocalGrainActivator(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }

    public override object Create(IGrainActivationContext context)
    {
        var grain = base.Create(context) as Grain;
        if (grain is FunctionGrain functionGrain)
        {
            context.ObservableLifecycle.Subscribe(this.GetType().FullName + "/Local",
                GrainLifecycleStage.Activate,
                ct => OnActivateAsync(context, functionGrain), ct => OnDeactivateAsync(context, functionGrain));
        }
        return grain;
    }

    private Task OnDeactivateAsync(IGrainActivationContext grainActivationContext, FunctionGrain functionGrain)
    {
        if (grainActivationContext.GrainIdentity != null)
            localGrains.TryRemove(grainActivationContext.GrainIdentity, out var _);
        return Task.CompletedTask;
    }

    private Task OnActivateAsync(IGrainActivationContext grainActivationContext, FunctionGrain functionGrain)
    {
        var scheduler = TaskScheduler.Current;
        localGrains.TryAdd(grainActivationContext.GrainIdentity, new FunctionGrainContext(scheduler, functionGrain));
        return Task.CompletedTask;
    }

    public FunctionGrainContext GetGrain(IGrainIdentity getGrainIdentity)
    {
        localGrains.TryGetValue(getGrainIdentity, out var grainContext);
        return grainContext;
    }
}

public interface ILocalGrainCatalog
{
    FunctionGrainContext GetGrain(IGrainIdentity getGrainIdentity);
}
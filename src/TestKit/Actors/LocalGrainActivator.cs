using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Orleans;
using Orleans.Core;
using Orleans.Runtime;

namespace TestKit.Actors;

public class LocalGrainActivator : DefaultGrainActivator, ILocalGrainCatalog
{
    private ConcurrentDictionary<IGrainIdentity, FunctionGrainContext> localGrains = new();

    public LocalGrainActivator(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }

    public override object Create(IGrainActivationContext context)
    {
        var grain = base.Create(context) as Grain;
        if (grain is FunctionInstanceGrain functionGrain)
        {
            context.ObservableLifecycle.Subscribe(this.GetType().FullName + "/Local",
                GrainLifecycleStage.Activate,
                ct => OnActivateAsync(context, functionGrain), ct => OnDeactivateAsync(context, functionGrain));
        }
        return grain;
    }

    private Task OnDeactivateAsync(IGrainActivationContext grainActivationContext, FunctionInstanceGrain functionInstanceGrain)
    {
        if (grainActivationContext.GrainIdentity != null)
            localGrains.TryRemove(grainActivationContext.GrainIdentity, out var _);
        return Task.CompletedTask;
    }

    private Task OnActivateAsync(IGrainActivationContext grainActivationContext, FunctionInstanceGrain functionInstanceGrain)
    {
        var scheduler = TaskScheduler.Current;
        localGrains.TryAdd(grainActivationContext.GrainIdentity, new FunctionGrainContext(scheduler, functionInstanceGrain));
        return Task.CompletedTask;
    }

    public FunctionGrainContext GetGrain(IGrainIdentity getGrainIdentity)
    {
        localGrains.TryGetValue(getGrainIdentity, out var grainContext);
        return grainContext;
    }
}
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Orleans;
using Orleans.Concurrency;

namespace FunctionTestHost.Actors;

public interface IFunctionEndpointGrain : IGrainWithStringKey
{
    Task Add(IFunctionInstanceGrain functionInstanceGrain);
    Task Call();
}

[Reentrant]
public class FunctionEndpointGrain : Grain, IFunctionEndpointGrain
{
    private List<IFunctionInstanceGrain> grains = new();
    private TaskCompletionSource init = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public Task Add(IFunctionInstanceGrain functionInstanceGrain)
    {
        grains.Add(functionInstanceGrain);
        init.TrySetResult();
        return Task.CompletedTask;
    }

    public async Task Call()
    {
        await init.Task;
        if (grains.Any())
            await grains.First().Call(this.GetPrimaryKeyString());
    }
}
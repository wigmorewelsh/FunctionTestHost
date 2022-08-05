using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Orleans;
using Orleans.Concurrency;

namespace TestKit.Actors;

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

    public async Task<AzureFunctionsRpcMessages.InvocationResponse> Call()
    {
        await init.Task;
        if (grains.Any())
            return await grains.First().Request(this.GetPrimaryKeyString());
        else
        {
            throw new NotSupportedException("No functions avaliable");
        }
    }

    public async Task<AzureFunctionsRpcMessages.InvocationResponse> Call(AzureFunctionsRpcMessages.RpcHttp body)
    {
        await init.Task;
        if (grains.Any())
            return await grains.First().RequestHttpRequest(this.GetPrimaryKeyString(), body);
        else
        {
            throw new NotSupportedException("No functions avaliable");
        }
    }
}
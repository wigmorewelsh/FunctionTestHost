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
    private List<(string functionId, IFunctionInstanceGrain functionInstanceGrain)> grains = new();
    private TaskCompletionSource init = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public Task Add(string functionId, IFunctionInstanceGrain functionInstanceGrain)
    {
        grains.Add((functionId, functionInstanceGrain));
        init.TrySetResult();
        return Task.CompletedTask;
    }

    public async Task<AzureFunctionsRpcMessages.InvocationResponse> Call(AzureFunctionsRpcMessages.RpcHttp body)
    {
        await init.Task;
        if (grains.Any())
        {
            var (functionId, grain) = grains.First();
            return await grain.RequestHttpRequest(functionId, body);
        }
        else
        {
            throw new NotSupportedException("No functions avaliable");
        }
    }
}
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using AzureFunctionsRpcMessages;
using Grpc.Core;
using Orleans;
using Orleans.Concurrency;
using Orleans.Placement;
using Orleans.Runtime;
using StreamingMessage = FunctionMetadataEndpoint.StreamingMessage;

namespace FunctionTestHost.Actors;

[PreferLocalPlacement]
[Reentrant]
public class FunctionInstanceGrain : Grain, IFunctionInstanceGrain
{
    private readonly IGrainActivationContext _context;

    public FunctionInstanceGrain(IGrainActivationContext context)
    {
        _context = context;
    }

    public TaskCompletionSource<IServerStreamWriter<AzureFunctionsRpcMessages.StreamingMessage>> ResponseStream =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    public TaskCompletionSource ReadyForRequests =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private CancellationTokenSource _shutdown = new ();

    public override Task OnActivateAsync()
    {
        return Task.CompletedTask;
    }

    public override Task OnDeactivateAsync()
    {
        ResponseStream.TrySetCanceled();
        ReadyForRequests.TrySetCanceled();
        pendingRequest.TrySetCanceled();
        _shutdown.Cancel();
        return base.OnDeactivateAsync();
    }

    private FunctionState State = FunctionState.Init;

    public Task Init()
    {
        State = FunctionState.Init;
        return Task.CompletedTask;
    }

    private List<AzureFunctionsRpcMessages.FunctionLoadRequest> _bindings = new();

    public async Task InitMetadata(FunctionMetadataEndpoint.StreamingMessage message)
    {
        var stream = await ResponseStream.Task;
        foreach (var loadRequest in message.FunctionInit.FunctionLoadRequestsResults)
        {
            _bindings.Add(loadRequest);
            await stream.WriteAsync(new AzureFunctionsRpcMessages.StreamingMessage
            {
                RequestId = Guid.NewGuid().ToString(),
                FunctionLoadRequest = loadRequest
            });
            var endpointGrain = GrainFactory.GetGrain<IFunctionEndpointGrain>(loadRequest.Metadata.Name);
            await endpointGrain.Add(this.AsReference<IFunctionInstanceGrain>());
        }
    }

    public async Task SetReady()
    {
        ReadyForRequests.TrySetResult();
    }

    private TaskCompletionSource<InvocationResponse> pendingRequest = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public async Task<InvocationResponse> Request(string functionId)
    {
        await ReadyForRequests.Task;
        var stream = await ResponseStream.Task;

        var rpcTraceContext = new RpcTraceContext
        {
            TraceParent = "123"
        };
        var streamingMessage = new AzureFunctionsRpcMessages.StreamingMessage
        {
            RequestId = Guid.NewGuid().ToString(),
            InvocationRequest = new InvocationRequest()
            {
                FunctionId = functionId,
                InvocationId = "123",
                InputData =
                {
                    new ParameterBinding
                    {
                        Name = "req",
                        Data = new TypedData
                        {
                            Http = new RpcHttp
                            {

                            }
                        }
                    }
                },
                TraceContext = rpcTraceContext
            }
        };
        await stream.WriteAsync(streamingMessage);
        return await pendingRequest.Task;
    }

    public Task Response(InvocationResponse response)
    {
        pendingRequest.TrySetResult(response);
        return Task.CompletedTask;
    }

    public Task Notification()
    {
        return Task.CompletedTask;
    }
}
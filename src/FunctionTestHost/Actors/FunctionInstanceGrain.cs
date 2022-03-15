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
        foreach (var pendingRequestsValue in pendingRequests.Values)
        {
            pendingRequestsValue.TrySetCanceled();
        }
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
    private Dictionary<string, string> _httpBindings = new ();

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
            // TODO: extract this into external class and config
            if (TryGetHttpBinding(loadRequest, out var paramName, out var httpBinding))
            {
                var endpointGrain = GrainFactory.GetGrain<IFunctionEndpointGrain>(loadRequest.Metadata.Name);
                await endpointGrain.Add(this.AsReference<IFunctionInstanceGrain>());
                _httpBindings[loadRequest.FunctionId] = paramName;
            }
        }
    }

    private bool TryGetHttpBinding(FunctionLoadRequest loadRequest, out string bindingName, out BindingInfo bindingInfo)
    {
        foreach (var (key, value) in loadRequest.Metadata.Bindings)
        {
            if (value.Type == "HttpTrigger")
            {
                bindingName = key;
                bindingInfo = value;
                return true;
            }
        }

        bindingName = null;
        bindingInfo = null;
        return false;
    }

    public async Task SetReady()
    {
        ReadyForRequests.TrySetResult();
    }

    private Dictionary<Guid, TaskCompletionSource<InvocationResponse>> pendingRequests = new();

    public async Task<InvocationResponse> RequestHttpRequest(string functionId, RpcHttp body)
    {
        await ReadyForRequests.Task;
        var stream = await ResponseStream.Task;

        var task = new TaskCompletionSource<InvocationResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
        var taskId = Guid.NewGuid();
        pendingRequests[taskId] = task;
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
                InvocationId = taskId.ToString(),
                InputData =
                {
                    new ParameterBinding
                    {
                        Name = _httpBindings[functionId],
                        Data = new TypedData
                        {
                            Http = body
                        }
                    }
                },
                TraceContext = rpcTraceContext
            }
        };
        await stream.WriteAsync(streamingMessage);
        return await task.Task;
    }

    public async Task<InvocationResponse> Request(string functionId)
    {
        return await this.RequestHttpRequest(functionId, new RpcHttp());
    }

    public Task Response(InvocationResponse response)
    {
        if (Guid.TryParse(response.InvocationId, out var guid))
        {
            if (pendingRequests.TryGetValue(guid, out var taskCompletionSource))
            {
                taskCompletionSource.TrySetResult(response);
                pendingRequests.Remove(guid);
            }
        }
        return Task.CompletedTask;
    }

    public Task Notification()
    {
        return Task.CompletedTask;
    }
}
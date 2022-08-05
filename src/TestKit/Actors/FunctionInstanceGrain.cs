using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AzureFunctionsRpcMessages;
using Grpc.Core;
using Orleans;
using Orleans.Concurrency;
using Orleans.Placement;
using Orleans.Runtime;

namespace TestKit.Actors;

[PreferLocalPlacement]
[Reentrant]
public class FunctionInstanceGrain : Grain, IFunctionInstanceGrain
{
    private readonly IGrainActivationContext _context;
    private Dictionary<string, DataMapper> _dataMappers = new();

    public FunctionInstanceGrain(IGrainActivationContext context)
    {
        _context = context;
    }

    public TaskCompletionSource<IServerStreamWriter<AzureFunctionsRpcMessages.StreamingMessage>> ResponseStream =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    public TaskCompletionSource ReadyForRequests =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private CancellationTokenSource _shutdown = new();

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

    private readonly List<AzureFunctionsRpcMessages.FunctionLoadRequest> _bindings = new();

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
                _dataMappers.Add(loadRequest.FunctionId, new HttpDataMapper(paramName));
            }

            if (TryGetServiceBusBinding(loadRequest, out var paramsSbName, out var servicebusBinding))
            {
                var isBatch = servicebusBinding.Cardinality == BindingInfo.Types.Cardinality.Many;

                //TODO: subscribe to service bus grain
                _dataMappers.Add(loadRequest.FunctionId, new ServiceBusDataMapper(isBatch, paramsSbName));
            }
           
            var functionName = Path.GetFileNameWithoutExtension(loadRequest.Metadata.ScriptFile);
            var directEndpointGrain = GrainFactory.GetGrain<IFunctionEndpointGrain>(functionName + "/" + loadRequest.Metadata.Name);
            await directEndpointGrain.Add(this.AsReference<IFunctionInstanceGrain>());
            
            var adminEndpointGrain = GrainFactory.GetGrain<IFunctionAdminEndpointGrain>("admin/" + loadRequest.Metadata.Name);
            await adminEndpointGrain.Add(this.AsReference<IFunctionInstanceGrain>());
        }
    }

    private bool TryGetServiceBusBinding(FunctionLoadRequest loadRequest, out string bindingName,
        out BindingInfo bindingInfo)
    {
        foreach (var (key, value) in loadRequest.Metadata.Bindings)
        {
            if (value.Type == "ServiceBusTrigger")
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
        if (_dataMappers.TryGetValue(functionId, out var dataMapper))
        {
            var typedData = dataMapper.ToTypedData(functionId, body);
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
                            Name = dataMapper.ParamsName,
                            Data = typedData
                        }
                    },
                    TraceContext = rpcTraceContext
                }
            };
            await stream.WriteAsync(streamingMessage);
            return await task.Task;
        }

        throw new NotSupportedException($"cannot call function with: {functionId}");
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
}
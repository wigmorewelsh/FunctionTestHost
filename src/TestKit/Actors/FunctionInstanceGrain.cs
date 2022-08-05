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
    private readonly IEnumerable<IDataMapperFactory> _dataMapperFactories;
    private readonly Dictionary<string, DataMapper> _dataMappers = new();
    private readonly List<AzureFunctionsRpcMessages.FunctionLoadRequest> _bindings = new();
    private readonly Dictionary<Guid, TaskCompletionSource<InvocationResponse>> pendingRequests = new();
    
    private readonly CancellationTokenSource _shutdown = new();
    
    private FunctionState State = FunctionState.Init;

    public FunctionInstanceGrain(IGrainActivationContext context, IEnumerable<IDataMapperFactory> dataMapperFactories)
    {
        _context = context;
        _dataMapperFactories = dataMapperFactories;
    }

    public TaskCompletionSource<IServerStreamWriter<AzureFunctionsRpcMessages.StreamingMessage>> ResponseStream =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    public TaskCompletionSource ReadyForRequests =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

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

    public Task Init()
    {
        State = FunctionState.Init;
        return Task.CompletedTask;
    }

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

            var dataMapper = await TryCreateDataMapper(loadRequest);

            if (dataMapper == null) continue;
            _dataMappers.Add(loadRequest.FunctionId, dataMapper);

            var functionName = Path.GetFileNameWithoutExtension(loadRequest.Metadata.ScriptFile);
            var directEndpointGrain = GrainFactory.GetGrain<IFunctionEndpointGrain>(functionName + "/" + loadRequest.Metadata.Name);
            await directEndpointGrain.Add(this.AsReference<IFunctionInstanceGrain>());

            var adminEndpointGrain =
                GrainFactory.GetGrain<IFunctionAdminEndpointGrain>("admin/" + loadRequest.Metadata.Name);
            await adminEndpointGrain.Add(this.AsReference<IFunctionInstanceGrain>());
        }
    }

    private async Task<DataMapper?> TryCreateDataMapper(FunctionLoadRequest loadRequest)
    {
        foreach (var dataMapperFactory in _dataMapperFactories)
            if (await dataMapperFactory.TryCreateDataMapper(loadRequest, this) is { } dataMapper)
                return dataMapper;

        return null;
    }

    public async Task SetReady()
    {
        ReadyForRequests.TrySetResult();
    }

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
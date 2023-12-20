using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AzureFunctionsRpcMessages;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Concurrency;
using Orleans.Placement;
using Stateless;
using Stateless.Graph;
using TestKit.Services;

namespace TestKit.Actors;

public enum StateEnum
{
    Started,
    LoadingMetadata,
    LoadingFunctions,
    Ready,
    Running,
    Sleeping,
    Stopped
}

public enum TriggerEnum
{
    ObserverAdded,
    LoadedMetadata,
    LoadedFunctions,
    InvocationRequest,
    InvocationResponse,
    Stop,
    InvocationStarted,
    EmptyQueue
}

public class FunctionInvocationRequest
{
    private readonly TaskCompletionSource<InvocationResponse> _taskCompletionSource;
    public Guid TaskId { get; }
    public StreamingMessage StreamingMessage { get; }

    public Task<InvocationResponse> Task => _taskCompletionSource.Task;

    public FunctionInvocationRequest(Guid taskId, StreamingMessage streamingMessage)
    {
        TaskId = taskId;
        StreamingMessage = streamingMessage;
        _taskCompletionSource =
            new TaskCompletionSource<InvocationResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    public void Cancel()
    {
        _taskCompletionSource.TrySetCanceled();
    }

    public void Complete(InvocationResponse response)
    {
        _taskCompletionSource.TrySetResult(response);
    }
}

[PreferLocalPlacement]
[Reentrant]
internal class FunctionInstanceGrain : Grain, IFunctionInstanceGrain
{
    private readonly IEnumerable<IDataMapperFactory> _dataMapperFactories;

    // todo check for duplicates
    private readonly HashSet<RpcFunctionMetadata> _bindings = new();
    private readonly Dictionary<string, DataMapper> _dataMappers = new();

    private readonly List<FunctionInvocationRequest> _invocationRequests = new();
    
    private HashSet<IFunctionObserver> observers = new();

    private StateMachine<StateEnum, TriggerEnum> _stateMachine = new(StateEnum.Started, FiringMode.Immediate)
        { RetainSynchronizationContext = true };

    public FunctionInstanceGrain(ILogger<FunctionInstanceGrain> logger,
        IEnumerable<IDataMapperFactory> dataMapperFactories)
    {
        _dataMapperFactories = dataMapperFactories;

        _stateMachine.OnTransitioned((transition) =>
        {
            logger.LogInformation(
                $"State: {transition.Source} -> {transition.Destination} via {transition.Trigger}");
        });

        _stateMachine.Configure(StateEnum.Started)
            .Permit(TriggerEnum.ObserverAdded, StateEnum.LoadingMetadata)
            .Ignore(TriggerEnum.InvocationRequest)
            .Ignore(TriggerEnum.InvocationResponse);

        _stateMachine.Configure(StateEnum.LoadingMetadata)
            .PermitReentry(TriggerEnum.ObserverAdded)
            .Permit(TriggerEnum.LoadedMetadata, StateEnum.LoadingFunctions)
            .Ignore(TriggerEnum.InvocationRequest)
            .Ignore(TriggerEnum.InvocationResponse)
            .OnEntryAsync(() => this.FetchMetaData());

        _stateMachine.Configure(StateEnum.LoadingFunctions)
            .Permit(TriggerEnum.LoadedFunctions, StateEnum.Ready)
            .Ignore(TriggerEnum.InvocationRequest)
            .Ignore(TriggerEnum.InvocationResponse)
            .OnEntryAsync(() => this.LoadFunctions())
            .OnExitAsync(() => this.SetReady());

        _stateMachine.Configure(StateEnum.Ready)
            .Permit(TriggerEnum.InvocationStarted, StateEnum.Running)
            .Permit(TriggerEnum.EmptyQueue, StateEnum.Sleeping)
            .Ignore(TriggerEnum.LoadedFunctions)
            .OnEntryAsync(() => this.ProcessMessage());

        _stateMachine.Configure(StateEnum.Running)
            .Permit(TriggerEnum.InvocationResponse, StateEnum.Ready);

        _stateMachine.Configure(StateEnum.Sleeping)
            .Permit(TriggerEnum.InvocationRequest, StateEnum.Ready);
    }

    private async Task SetReady()
    {
        var registry = GrainFactory.GetGrain<IFunctionRegistoryGrain>(0);
        await registry.UpdateFunction(this.GetPrimaryKeyString());
    }

    public async override Task OnActivateAsync()
    {
        var registry = GrainFactory.GetGrain<IFunctionRegistoryGrain>(0);
        await registry.RegisterFunction(this.GetPrimaryKeyString());
    }
    
    public override Task OnDeactivateAsync()
    {
        foreach (var pendingRequestsValue in _invocationRequests)
        {
            pendingRequestsValue.Cancel();
        }

        return Task.CompletedTask;
    }

    private async Task ProcessMessage()
    {
        if (_invocationRequests.Any())
        {
            var invocationRequest = _invocationRequests.First();
            await Send(invocationRequest.StreamingMessage);
            await _stateMachine.FireAsync(TriggerEnum.InvocationStarted);
        }
        else
        {
            await _stateMachine.FireAsync(TriggerEnum.EmptyQueue);
        }
    }

    public async Task InitMetadata(AzureFunctionsRpcMessages.FunctionMetadataResponse message)
    {
        foreach (var loadRequest in message.FunctionMetadataResults)
        {
            _bindings.Add(loadRequest);

            var dataMapper = await TryCreateDataMapper(loadRequest);

            if (dataMapper == null && TryGetAnyBinding(loadRequest, out var bindingName, out var bindingInfo))
                dataMapper = new HttpDataMapper(bindingName, bindingInfo);
            if (dataMapper == null) continue;
            _dataMappers.Add(loadRequest.FunctionId, dataMapper);

            //named function endpoints for multi function hosting
            var functionName = Path.GetFileNameWithoutExtension(loadRequest.ScriptFile);
            var directEndpointGrain =
                GrainFactory.GetGrain<IFunctionEndpointGrain>(functionName + "/" + loadRequest.Name);
            await directEndpointGrain.Add(loadRequest.FunctionId, this.AsReference<IFunctionInstanceGrain>());

            //admin endpoints for direct calls
            var adminEndpointGrain = GrainFactory.GetGrain<IFunctionAdminEndpointGrain>("admin/" + loadRequest.Name);
            await adminEndpointGrain.Add(loadRequest.FunctionId, this.AsReference<IFunctionInstanceGrain>());
        }

        _stateMachine.FireAsync(TriggerEnum.LoadedMetadata).Ignore();
    }

    public async Task LoadFunctions()
    {
        foreach (var loadRequest in _bindings)
        {
            await Send(new AzureFunctionsRpcMessages.StreamingMessage
            {
                RequestId = Guid.NewGuid().ToString(),
                FunctionLoadRequest = new FunctionLoadRequest()
                {
                    FunctionId = loadRequest.FunctionId,
                    ManagedDependencyEnabled = loadRequest.ManagedDependencyEnabled,
                    Metadata = loadRequest
                }
            });
        }
    }

    private bool TryGetAnyBinding(RpcFunctionMetadata loadRequest, out string bindingName, out BindingInfo bindingInfo)
    {
        foreach (var (key, value) in loadRequest.Bindings)
        {
            bindingName = key;
            bindingInfo = value;
            return true;
        }

        bindingName = null;
        bindingInfo = null;
        return false;
    }

    private async Task<DataMapper?> TryCreateDataMapper(RpcFunctionMetadata loadRequest)
    {
        foreach (var dataMapperFactory in _dataMapperFactories)
            if (await dataMapperFactory.TryCreateDataMapper(loadRequest, this) is { } dataMapper)
                return dataMapper;

        return null;
    }

    public async Task<InvocationResponse> Request(string functionId, TypedData typedData)
    {
        if (_dataMappers.TryGetValue(functionId, out var dataMapper))
        {
            var taskId = Guid.NewGuid();
            var streamingMessage = ToInvocationRequest(functionId, typedData, taskId, dataMapper);
            var invocationRequest = new FunctionInvocationRequest(taskId, streamingMessage);
            _invocationRequests.Add(invocationRequest);

            _stateMachine.FireAsync(TriggerEnum.InvocationRequest).Ignore();

            return await invocationRequest.Task;
        }

        throw new NotSupportedException($"cannot call function with: {functionId}");
    }

    private static StreamingMessage ToInvocationRequest(string functionId, TypedData typedData, Guid taskId,
        DataMapper dataMapper)
    {
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
                        Name = dataMapper.ParamsName,
                        Data = typedData
                    }
                },
                TraceContext = rpcTraceContext
            }
        };
        return streamingMessage;
    }

    public async Task Subscribe(IFunctionObserver observerRef)
    {
        observers.Add(observerRef);
        _stateMachine.FireAsync(TriggerEnum.ObserverAdded).Ignore();
    }

    public async Task FetchMetaData()
    {
        await Send(new StreamingMessage()
        {
            FunctionsMetadataRequest = new FunctionsMetadataRequest()
            {
            }
        });
    }

    public Task Recieve(StreamingMessage message)
    {
        if (message.FunctionMetadataResponse is { } functionMetadataResponse &&
            functionMetadataResponse.Result.Status is StatusResult.Types.Status.Success)
            return InitMetadata(functionMetadataResponse);

        if (message.InvocationResponse is { } invocationResponse)
            return Response(invocationResponse);

        if (message.FunctionLoadResponse is { } functionLoadResponse)
        {
            _stateMachine.FireAsync(TriggerEnum.LoadedFunctions).Ignore();
        }

        return Task.CompletedTask;
    }

    public Task Ping()
    {
        return Task.CompletedTask;
    }

    public async Task<InvocationResponse> RequestHttpRequest(string functionId, RpcHttp body)
    {
        if (_dataMappers.TryGetValue(functionId, out var dataMapper))
        {
            var typedData = dataMapper.ToTypedData(functionId, body);

            var taskId = Guid.NewGuid();
            var streamingMessage = ToInvocationRequest(functionId, typedData, taskId, dataMapper);
            var invocationRequest = new FunctionInvocationRequest(taskId, streamingMessage);
            _invocationRequests.Add(invocationRequest);

            _stateMachine.FireAsync(TriggerEnum.InvocationRequest).Ignore();

            return await invocationRequest.Task;
        }

        throw new NotSupportedException($"cannot call function with: {functionId}");
    }

    private async Task Send(StreamingMessage message)
    {
        foreach (var observer in observers)
        {
            await observer.Send(message);
        }
    }

    public async Task Response(InvocationResponse response)
    {
        if (Guid.TryParse(response.InvocationId, out var guid))
        {
            foreach (var invocationRequest in _invocationRequests.ToList())
            {
                invocationRequest.Complete(response);
                _invocationRequests.Remove(invocationRequest);
            }
        }

        _stateMachine.FireAsync(TriggerEnum.InvocationResponse).Ignore();
    }
}
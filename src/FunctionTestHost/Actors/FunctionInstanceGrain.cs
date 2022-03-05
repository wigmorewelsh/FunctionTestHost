using System;
using System.ComponentModel.DataAnnotations;
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

    public override Task OnActivateAsync()
    {
        return Task.CompletedTask;
    }

    private FunctionState State = FunctionState.Init;

    public Task Init()
    {
        State = FunctionState.Init;
        return Task.CompletedTask;
    }

    public async Task InitMetadata(byte[] message)
    {
        var messagePar = StreamingMessage.Parser.ParseFrom(message);
        var stream = await ResponseStream.Task;
        foreach (var loadRequest in messagePar.FunctionInit.FunctionLoadRequestsResults)
        {
            // await responseStream.WriteAsync(new StreamingMessage
            // {
            //     FunctionLoadRequest = new FunctionLoadRequest
            //     {
            //         FunctionId = "Hello",
            //         Metadata = new RpcFunctionMetadata
            //         {
            //             IsProxy = false,
            //             ScriptFile = "FunctionAppOne.dll",
            //             Name = "Hello",
            //             EntryPoint = "FunctionAppOne.Hello.Run",
            //         }
            //     }
            // });

            await stream.WriteAsync(new AzureFunctionsRpcMessages.StreamingMessage
            {
                RequestId = Guid.NewGuid().ToString(),
                FunctionLoadRequest = new FunctionLoadRequest
                {
                    FunctionId = loadRequest.FunctionId,
                    // ManagedDependencyEnabled = loadRequest.ManagedDependencyEnabled,
                    Metadata = new RpcFunctionMetadata
                    {
                        Name = loadRequest.Metadata.Name,
                        Directory = loadRequest.Metadata.Directory,
                        EntryPoint = loadRequest.Metadata.EntryPoint,
                        IsProxy = loadRequest.Metadata.IsProxy,
                        ScriptFile = loadRequest.Metadata.ScriptFile
                    }
                }
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
        await stream.WriteAsync(new AzureFunctionsRpcMessages.StreamingMessage
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
                TraceContext = new RpcTraceContext
                {
                    TraceParent = "123"
                }
            }
        });
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
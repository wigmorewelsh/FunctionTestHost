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
public class FunctionGrain : Grain, IFunctionGrain
{
    private readonly ConnectionManager _manager;
    private readonly IGrainActivationContext _context;

    public FunctionGrain(ConnectionManager manager, IGrainActivationContext context)
    {
        _manager = manager;
        _context = context;
    }

    public TaskCompletionSource<IServerStreamWriter<AzureFunctionsRpcMessages.StreamingMessage>> ResponseStream = new (TaskCreationOptions.RunContinuationsAsynchronously);

    public override Task OnActivateAsync()
    {
        _grpcChannel = _manager.Lookup(this.GetPrimaryKeyString());
        return Task.CompletedTask;
    }

    private FunctionState State = FunctionState.Init;
    private ChannelWriter<AzureFunctionsRpcMessages.StreamingMessage> _grpcChannel;

    public Task Init()
    {
        State = FunctionState.Init;
        return Task.CompletedTask;
    }

    public async Task InitMetadata(byte[] message)
    {
        var messagePar = StreamingMessage.Parser.ParseFrom(message);
        foreach (var loadRequest in messagePar.FunctionInit.FunctionLoadRequestsResults)
        {
            await _grpcChannel.WriteAsync(new AzureFunctionsRpcMessages.StreamingMessage
            {
                RequestId = Guid.NewGuid().ToString(),
                FunctionLoadRequest = new FunctionLoadRequest
                {
                    FunctionId = loadRequest.FunctionId,
                    ManagedDependencyEnabled = loadRequest.ManagedDependencyEnabled,
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
        }
    }

    public async Task Call()
    {
        var stream = await ResponseStream.Task;
        await stream.WriteAsync(new AzureFunctionsRpcMessages.StreamingMessage
        {
            RequestId = Guid.NewGuid().ToString(),
            InvocationRequest = new InvocationRequest()
            {
                FunctionId = "Hello",
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
    }

    public Task Notification()
    {
        return Task.CompletedTask;
    }
}
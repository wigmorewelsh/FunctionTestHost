using System;
using System.Threading;
using System.Threading.Tasks;
using AzureFunctionsRpcMessages;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Orleans;
using TestKit.Actors;

namespace TestKit.Services;

internal partial class FunctionRpcService : FunctionRpc.FunctionRpcBase
{
    private readonly IGrainFactory _grainFactory;
    private readonly ILogger<FunctionRpcService> _logger;

    public FunctionRpcService(IGrainFactory grainFactory, ILogger<FunctionRpcService> logger)
    {
        _grainFactory = grainFactory;
        _logger = logger;
    }

    public override async Task EventStream(IAsyncStreamReader<StreamingMessage> requestStream,
        IServerStreamWriter<StreamingMessage> responseStream, ServerCallContext context)
    {
        var (workerId, functionGrain) = await SetupFunctionGrain(requestStream, context.CancellationToken);
        
        var observer = new FunctionRpcObserver(requestStream, responseStream, functionGrain);
        var observerRef = await _grainFactory.CreateObjectReference<IFunctionObserver>(observer);
        
        await functionGrain.Subscribe(observerRef);
       
        await observer.ForwardToGrain(context.CancellationToken); 
    }

    private async Task<(string, IFunctionInstanceGrain)> SetupFunctionGrain(
        IAsyncStreamReader<StreamingMessage> requestStream, CancellationToken contextCancellationToken)
    {
        if (await requestStream.MoveNext(contextCancellationToken))
        {
            var nextMessage = requestStream.Current;
            if (nextMessage.StartStream is { } initRequest)
            {
                return (initRequest.WorkerId, _grainFactory.GetGrain<IFunctionInstanceGrain>(initRequest.WorkerId));
            }
        }

        throw new Exception("Expected StartStream message");
    }
}
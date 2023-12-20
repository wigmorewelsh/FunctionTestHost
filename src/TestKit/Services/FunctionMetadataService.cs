using System;
using System.Threading;
using System.Threading.Tasks;
using FunctionMetadataEndpoint;
using Grpc.Core;
using Orleans;
using TestKit.Actors;

namespace TestKit.Services;

internal class FunctionMetadataService : FunctionRpc.FunctionRpcBase
{
    private readonly IGrainFactory _grainFactory;

    public FunctionMetadataService(IGrainFactory grainFactory)
    {
        _grainFactory = grainFactory;
    }
        
    public override async Task EventStream(IAsyncStreamReader<StreamingMessage> requestStream, IServerStreamWriter<StreamingMessage> responseStream, ServerCallContext context)
    {
        var (workerId, grain) = await SetupFunctionGrain(requestStream, context.CancellationToken);
        
        var observer = new FunctionMetadataObserver(requestStream, responseStream, grain);
#if NET6_0
        var observerRef = await _grainFactory.CreateObjectReference<IFunctionObserver>(observer);
#else 
        var observerRef = _grainFactory.CreateObjectReference<IFunctionObserver>(observer);
#endif
        
        await grain.Subscribe(observerRef);
        
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
using System;
using System.Threading;
using System.Threading.Tasks;
using AzureFunctionsRpcMessages;
using FunctionTestHost.Actors;
using Grpc.Core;
using Orleans;

namespace FunctionTestHost;

public class FunctionRpcService : FunctionRpc.FunctionRpcBase
{
    private readonly IGrainFactory _grainFactory;
    private readonly ILocalGrainCatalog _localGrainCatalog;

    public FunctionRpcService(IGrainFactory grainFactory, ILocalGrainCatalog localGrainCatalog)
    {
        _grainFactory = grainFactory;
        _localGrainCatalog = localGrainCatalog;
    }

    public override async Task EventStream(IAsyncStreamReader<StreamingMessage> requestStream,
        IServerStreamWriter<StreamingMessage> responseStream, ServerCallContext context)
    {
        var (workerId, functionGrain) = await SetupFunctionGrain(requestStream, context.CancellationToken);

        await functionGrain.Init();
        var localGrain = _localGrainCatalog.GetGrain(functionGrain.GetGrainIdentity());
        localGrain.SetResponseStream(responseStream);

        var response = await WaitTillInit(requestStream, context.CancellationToken);

        await StartReading(requestStream, context.CancellationToken);
    }

    private async Task StartReading(IAsyncStreamReader<StreamingMessage> requestStream,
        CancellationToken contextCancellationToken)
    {
        while (await requestStream.MoveNext(contextCancellationToken))
        {
            var dd = requestStream.Current;
            if (dd.InvocationResponse is { } response)
            {
                var gg = response;
                return;
            }
        }
    }

    private async Task<FunctionLoadResponse> WaitTillInit(IAsyncStreamReader<StreamingMessage> requestStream,
        CancellationToken contextCancellationToken)
    {
        while (await requestStream.MoveNext(contextCancellationToken))
        {
            var nextMessage = requestStream.Current;
            if (nextMessage.FunctionLoadResponse is { } initRequest)
            {
                return initRequest;
            }
        }

        throw new Exception("Expected StartStream message");
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
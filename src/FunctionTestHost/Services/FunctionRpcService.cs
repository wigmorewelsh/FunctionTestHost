using System;
using System.Threading;
using System.Threading.Tasks;
using AzureFunctionsRpcMessages;
using FunctionTestHost.Actors;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Orleans;

namespace FunctionTestHost.Services;

public class FunctionRpcService : FunctionRpc.FunctionRpcBase
{
    private readonly IGrainFactory _grainFactory;
    private readonly ILocalGrainCatalog _localGrainCatalog;
    private readonly ILogger<FunctionRpcService> _logger;

    public FunctionRpcService(IGrainFactory grainFactory, ILocalGrainCatalog localGrainCatalog, ILogger<FunctionRpcService> logger)
    {
        _grainFactory = grainFactory;
        _localGrainCatalog = localGrainCatalog;
        _logger = logger;
    }

    public override async Task EventStream(IAsyncStreamReader<StreamingMessage> requestStream,
        IServerStreamWriter<StreamingMessage> responseStream, ServerCallContext context)
    {
        var (workerId, functionGrain) = await SetupFunctionGrain(requestStream, context.CancellationToken);

        await functionGrain.Init();
        var localGrain = _localGrainCatalog.GetGrain(functionGrain.GetGrainIdentity());
        localGrain.SetResponseStream(responseStream);

        var response = await WaitTillInit(requestStream, context.CancellationToken);

        // localGrain.SetRequestStream(responseStream); ??
        await functionGrain.SetReady();

        await StartReading(requestStream, functionGrain, context.CancellationToken);
        // await localGrain.DeactivationTask(context.CancellationToken); ??
    }

    private async Task StartReading(IAsyncStreamReader<StreamingMessage> requestStream,
        IFunctionInstanceGrain functionInstanceGrain,
        CancellationToken contextCancellationToken)
    {
        while (await requestStream.MoveNext(contextCancellationToken))
        {
            var dd = requestStream.Current;
            if (dd.InvocationResponse is { } response)
            {
                await functionInstanceGrain.Response(response);
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

            if (nextMessage.RpcLog is { } rpcLog)
            {
                _logger.LogInformation(rpcLog.Message);
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
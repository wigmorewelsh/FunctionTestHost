using System;
using System.Threading.Tasks;
using AzureFunctionsRpcMessages;
using FunctionTestHost.Actors;
using Grpc.Core;
using Orleans;

namespace FunctionTestHost
{
    public class FunctionRpcService : FunctionRpc.FunctionRpcBase
    {
        private readonly IGrainFactory _grainFactory;

        public FunctionRpcService(IGrainFactory grainFactory)
        {
            _grainFactory = grainFactory;
        }

        public override async Task EventStream(IAsyncStreamReader<StreamingMessage> requestStream,
            IServerStreamWriter<StreamingMessage> responseStream, ServerCallContext context)
        {
            var functionGrain = await SetupFunctionGrain(requestStream);
            await functionGrain.Init();
        }

        private async Task<IFunctionGrain> SetupFunctionGrain(IAsyncStreamReader<StreamingMessage> requestStream)
        {
            if (await requestStream.MoveNext())
            {
                var nextMessage = requestStream.Current;
                if (nextMessage.StartStream is { } initRequest)
                {
                    return _grainFactory.GetGrain<IFunctionGrain>(initRequest.WorkerId);
                }
            }

            throw new Exception("Expected StartStream message");
        }
    }
}
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
        await foreach (var message in requestStream.ReadAllAsync(context.CancellationToken))
        {
            var grain = _grainFactory.GetGrain<IFunctionInstanceGrain>(message.WorkerId);
            await grain.InitMetadata(message);
        }
    }
}
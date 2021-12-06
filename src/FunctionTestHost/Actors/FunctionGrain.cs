using System.Threading.Channels;
using System.Threading.Tasks;
using AzureFunctionsRpcMessages;
using Grpc.Core;
using Orleans;
using Orleans.Placement;
using StreamingMessage = FunctionMetadataEndpoint.StreamingMessage;

namespace FunctionTestHost.Actors
{
    [PreferLocalPlacement]
    public class FunctionGrain : Grain, IFunctionGrain
    {
        private readonly ConnectionManager _manager;

        public FunctionGrain(ConnectionManager manager)
        {
            _manager = manager;
            _grpcChannel = manager.Lookup(this.GetPrimaryKeyString());
        }
        
        private FunctionState State = FunctionState.Init;
        private readonly ChannelWriter<StreamingMessage> _grpcChannel;

        public Task Init()
        {
            State = FunctionState.Init;
            return Task.CompletedTask;
        }

        public async Task InitMetadata(StreamingMessage message)
        {
            await _grpcChannel.WriteAsync(message);
        }

        public Task Notification()
        {
            throw new System.NotImplementedException();
        }
    }
}
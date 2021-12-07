using System;
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
        }

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

        public Task Notification()
        {
            return Task.CompletedTask;
        }
    }
}
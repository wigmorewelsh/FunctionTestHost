using System.Threading.Tasks;
using AzureFunctionsRpcMessages;
using FunctionTestHost.ServiceBusEmulator;
using Grpc.Core;
using Orleans;
using Orleans.Placement;
using StreamingMessage = FunctionMetadataEndpoint.StreamingMessage;

namespace FunctionTestHost.Actors
{
    public interface IFunctionGrain : IGrainWithStringKey, IQueueSubscriber
    {
        Task Init();
        Task InitMetadata(StreamingMessage message);
    }

    public enum FunctionState
    {
        Init, FetchingMetadata, LoadingFunctions, Running
    }
    
    [PreferLocalPlacement]
    public class FunctionGrain : Grain, IFunctionGrain
    {
        private FunctionState State = FunctionState.Init;
        public Task Init()
        {
            State = FunctionState.Init;
            return Task.CompletedTask;
        }

        public Task InitMetadata(StreamingMessage message)
        {
            throw new System.NotImplementedException();
        }

        public Task Notification()
        {
            throw new System.NotImplementedException();
        }
    }
}
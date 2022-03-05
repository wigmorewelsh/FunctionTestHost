using System.Threading.Tasks;
using AzureFunctionsRpcMessages;
using Grpc.Core;

namespace FunctionTestHost.Actors;

public class FunctionGrainContext
{
    private readonly TaskScheduler _scheduler;
    private readonly FunctionGrain _functionGrain;

    public FunctionGrainContext(TaskScheduler scheduler, FunctionGrain functionGrain)
    {
        _scheduler = scheduler;
        _functionGrain = functionGrain;
    }

    public void SetResponseStream(IServerStreamWriter<StreamingMessage> responseStream)
    {
        _functionGrain.ResponseStream.TrySetResult(responseStream);
    }
}
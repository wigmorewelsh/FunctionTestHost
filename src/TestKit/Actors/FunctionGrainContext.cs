using System.Threading.Tasks;
using AzureFunctionsRpcMessages;
using Grpc.Core;

namespace TestKit.Actors;

internal class FunctionGrainContext
{
    private readonly TaskScheduler _scheduler;
    private readonly FunctionInstanceGrain _functionInstanceGrain;

    public FunctionGrainContext(TaskScheduler scheduler, FunctionInstanceGrain functionInstanceGrain)
    {
        _scheduler = scheduler;
        _functionInstanceGrain = functionInstanceGrain;
    }

    public void SetResponseStream(IServerStreamWriter<StreamingMessage> responseStream)
    {
        _functionInstanceGrain.ResponseStream.TrySetResult(responseStream);
    }
}
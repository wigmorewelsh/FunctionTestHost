using System.Threading;
using System.Threading.Tasks;
using AzureFunctionsRpcMessages;
using Grpc.Core;
using TestKit.Actors;

namespace TestKit.Services;

internal class FunctionRpcObserver : IFunctionObserver
{
    private readonly IAsyncStreamReader<StreamingMessage> _requestStream;
    private readonly IServerStreamWriter<StreamingMessage> _responseStream;
    private readonly IFunctionInstanceGrain _functionGrain;

    public FunctionRpcObserver(IAsyncStreamReader<StreamingMessage> requestStream,
        IServerStreamWriter<StreamingMessage> responseStream, 
        IFunctionInstanceGrain functionGrain)
    {
        _requestStream = requestStream;
        _responseStream = responseStream;
        _functionGrain = functionGrain;
    }

    public async Task Send(StreamingMessage message)
    {
        await _responseStream.WriteAsync(message);
    }

    public async Task ForwardToGrain(CancellationToken cancellationToken = default)
    {
        await foreach(var message in _requestStream.ReadAllAsync())
        {
            await _functionGrain.Recieve(message);
        }
    }
}
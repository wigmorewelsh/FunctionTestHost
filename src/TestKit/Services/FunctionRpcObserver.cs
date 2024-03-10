using System;
using System.Threading;
using System.Threading.Tasks;
using AzureFunctionsRpcMessages;
using Grpc.Core;
using TestKit.Actors;

namespace TestKit.Services;

internal class FunctionRpcObserver : IFunctionObserver, IDisposable
{
    private readonly IAsyncStreamReader<StreamingMessage> _requestStream;
    private readonly IServerStreamWriter<StreamingMessage> _responseStream;
    private readonly IFunctionInstanceGrain _functionGrain;
    private readonly CancellationTokenSource _cancelationTokenSource;

    public FunctionRpcObserver(IAsyncStreamReader<StreamingMessage> requestStream,
        IServerStreamWriter<StreamingMessage> responseStream, 
        IFunctionInstanceGrain functionGrain)
    {
        _cancelationTokenSource = new CancellationTokenSource();
        _requestStream = requestStream;
        _responseStream = responseStream;
        _functionGrain = functionGrain;
    }

    public async Task Send(StreamingMessage message)
    {
        if (_cancelationTokenSource.IsCancellationRequested) return;
        await _responseStream.WriteAsync(message);
    }

    public async Task ForwardToGrain(CancellationToken cancellationToken = default)
    {
        var token = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cancelationTokenSource.Token).Token;
        try
        {
            await foreach (var message in _requestStream.ReadAllAsync(token))
            {
                await _functionGrain.Recieve(message);
            }
        }
        catch (Microsoft.AspNetCore.Connections.ConnectionAbortedException) {}
        catch (System.IO.IOException) {}
        catch (System.OperationCanceledException) { }
    }

    public void Dispose()
    {
        _cancelationTokenSource.Cancel();
    }
}
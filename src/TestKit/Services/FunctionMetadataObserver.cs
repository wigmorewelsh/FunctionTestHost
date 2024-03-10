using System;
using System.Threading;
using System.Threading.Tasks;
using FunctionMetadataEndpoint;
using Grpc.Core;
using TestKit.Actors;

namespace TestKit.Services;

internal class FunctionMetadataObserver : IFunctionObserver
{
    private readonly IAsyncStreamReader<StreamingMessage> _requestStream;
    private readonly IServerStreamWriter<StreamingMessage> _responseStream;
    private readonly IFunctionInstanceGrain _functionGrain;

    public FunctionMetadataObserver(IAsyncStreamReader<StreamingMessage> requestStream,
        IServerStreamWriter<StreamingMessage> responseStream, 
        IFunctionInstanceGrain functionGrain)
    {
        _requestStream = requestStream;
        _responseStream = responseStream;
        _functionGrain = functionGrain;
    }

    public async Task Send(AzureFunctionsRpcMessages.StreamingMessage message)
    {
        if (TryConvertToMessage(message, out StreamingMessage streamMessage))
        {
            _responseStream.WriteAsync(streamMessage);
        }
    }

    private bool TryConvertToMessage(AzureFunctionsRpcMessages.StreamingMessage message, out StreamingMessage o)
    {
        o = new StreamingMessage();
        if (message.FunctionsMetadataRequest is { } functionLoadRequest)
        {
            o.FunctionsMetadataRequest = functionLoadRequest;
            return true;
        }
        
        if (message.FunctionMetadataResponse is { } functionLoadResponse)
        {
            o.FunctionMetadataResponse = functionLoadResponse;
            return true;
        } 
        
        return false;
    }

    public async Task ForwardToGrain(CancellationToken cancellationToken = default)
    {
        try
        {
            await foreach (var message in _requestStream.ReadAllAsync(cancellationToken))
            {
                await _functionGrain.Recieve(ConvertToMessage(message));
            }
        }
        catch (OperationCanceledException) {}
        catch (System.IO.IOException e)
        {
            // Stream closed
        }
    }

    private static AzureFunctionsRpcMessages.StreamingMessage ConvertToMessage(StreamingMessage message)
    {
        var streamMessage = new AzureFunctionsRpcMessages.StreamingMessage();
        if (message.FunctionsMetadataRequest is { } functionLoadRequest)
        {
            streamMessage.FunctionsMetadataRequest = functionLoadRequest;
        }
        
        if (message.FunctionMetadataResponse is { } functionLoadResponse)
        {
            streamMessage.FunctionMetadataResponse = functionLoadResponse;
        }

        return streamMessage;
    }
}
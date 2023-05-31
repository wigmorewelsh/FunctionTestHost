using System;
using System.Threading.Tasks;
using AzureFunctionsRpcMessages;
using Google.Protobuf;
using Orleans;

namespace TestKit.Actors;

public interface IFunctionInstanceGrain : IGrainWithStringKey 
{
    Task Init();
    Task InitMetadata(FunctionMetadataEndpoint.StreamingMessage message);

    // Full name needed for code gen
    Task Response(AzureFunctionsRpcMessages.InvocationResponse response);
    Task SetReady();
    Task<AzureFunctionsRpcMessages.InvocationResponse> RequestHttpRequest(string functionId, AzureFunctionsRpcMessages.RpcHttp body);
    Task<AzureFunctionsRpcMessages.InvocationResponse> Request(string functionId, AzureFunctionsRpcMessages.TypedData typedData);
}

[Serializable]
internal class Envelope<T> where T : IMessage<T>
{
    private byte[] _bytes;

    private Envelope(){}
    public Envelope(T message)
    {
        _bytes = message.ToByteArray();
    }

    public T ReadMessage()
    {
        var result = Activator.CreateInstance<T>();
        result.MergeFrom(_bytes);
        return result;
    }
}
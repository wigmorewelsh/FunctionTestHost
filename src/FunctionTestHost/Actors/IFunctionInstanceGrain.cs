using System;
using System.Threading.Tasks;
using FunctionMetadataEndpoint;
using FunctionTestHost.ServiceBusEmulator;
using Google.Protobuf;
using Orleans;
using Orleans.CodeGeneration;

namespace FunctionTestHost.Actors;

public interface IFunctionInstanceGrain : IGrainWithStringKey, IQueueSubscriber
{
    Task Init();
    Task InitMetadata(FunctionMetadataEndpoint.StreamingMessage message);
    Task<AzureFunctionsRpcMessages.InvocationResponse> Request(string functionId);
    // Full name needed for code gen
    Task Response(AzureFunctionsRpcMessages.InvocationResponse response);
    Task SetReady();
}

[Serializable]
public class Envelope<T> where T : IMessage<T>
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
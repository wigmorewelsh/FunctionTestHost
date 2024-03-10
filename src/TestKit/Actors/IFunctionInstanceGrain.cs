using System.Threading.Tasks;
using AzureFunctionsRpcMessages;
using Orleans;
using Orleans.Concurrency;
using TestKit.Services;
using StreamingMessage = FunctionMetadataEndpoint.StreamingMessage;

namespace TestKit.Actors;

public interface IFunctionInstanceGrain : IGrainWithStringKey 
{
    [AlwaysInterleave]
    Task<AzureFunctionsRpcMessages.InvocationResponse> RequestHttpRequest(string functionId, AzureFunctionsRpcMessages.RpcHttp body);
    [AlwaysInterleave]
    Task<AzureFunctionsRpcMessages.InvocationResponse> Request(string functionId, AzureFunctionsRpcMessages.TypedData typedData);
    Task Subscribe(IFunctionObserver observerRef);
    Task Recieve(AzureFunctionsRpcMessages.StreamingMessage message);
    Task Ping();
}
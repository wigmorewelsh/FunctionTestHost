using System.Threading.Tasks;
using AzureFunctionsRpcMessages;
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
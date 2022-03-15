using System.Threading.Tasks;
using AzureFunctionsRpcMessages;
using Orleans;

namespace FunctionTestHost.Actors;

public interface IFunctionEndpointGrain : IGrainWithStringKey
{
    Task Add(IFunctionInstanceGrain functionInstanceGrain);
    Task<AzureFunctionsRpcMessages.InvocationResponse> Call();
    Task<AzureFunctionsRpcMessages.InvocationResponse> Call(AzureFunctionsRpcMessages.RpcHttp body);
}
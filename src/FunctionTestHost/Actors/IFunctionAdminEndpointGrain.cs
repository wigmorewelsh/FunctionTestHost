using System.Threading.Tasks;
using Orleans;

namespace FunctionTestHost.Actors;

public interface IPublicEndpoint
{
    Task<AzureFunctionsRpcMessages.InvocationResponse> Call();
    Task<AzureFunctionsRpcMessages.InvocationResponse> Call(AzureFunctionsRpcMessages.RpcHttp body);
}

public interface IFunctionAdminEndpointGrain : IPublicEndpoint, IGrainWithStringKey
{
    Task Add(IFunctionInstanceGrain functionInstanceGrain);

}
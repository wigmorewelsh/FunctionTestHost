using System.Threading.Tasks;
using Orleans;

namespace TestKit.Actors;

public interface IPublicEndpoint
{
    Task<AzureFunctionsRpcMessages.InvocationResponse> Call(AzureFunctionsRpcMessages.RpcHttp body);
}

public interface IFunctionAdminEndpointGrain : IPublicEndpoint, IGrainWithStringKey
{
    Task Add(string functionId, IFunctionInstanceGrain functionInstanceGrain);
}
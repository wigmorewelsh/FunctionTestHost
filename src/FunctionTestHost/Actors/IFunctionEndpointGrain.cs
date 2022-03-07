using System.Threading.Tasks;
using Orleans;

namespace FunctionTestHost.Actors;

public interface IFunctionEndpointGrain : IGrainWithStringKey
{
    Task Add(IFunctionInstanceGrain functionInstanceGrain);
    Task<AzureFunctionsRpcMessages.InvocationResponse> Call();
}